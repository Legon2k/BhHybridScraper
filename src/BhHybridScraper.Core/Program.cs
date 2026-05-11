using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using HtmlAgilityPack;

// Ensure console handles UTF-8 characters properly
Console.OutputEncoding = System.Text.Encoding.UTF8;

// List of products to scrape
var urls = new List<string>
{
    "https://www.bhphotovideo.com/c/product/1955563-REG/samsung_sm_x230nzatxar_11_galaxy_tab_a11.html",
    "https://www.bhphotovideo.com/c/product/1850005-REG/lenovo_zadg0016us_11_tab_m11_multi_touch.html",
    "https://www.bhphotovideo.com/c/product/1898558-REG/samsung_sm_x920nzaaxar_14_6_galaxy_tab_s10.html"
};

// =====================================================================
// DOCKER / REMOTE ENVIRONMENT SETUP
// =====================================================================
bool isRunningInDocker = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

string? cdpUrl = Environment.GetEnvironmentVariable("CDP_URL");

if (string.IsNullOrEmpty(cdpUrl))
{
    if (isRunningInDocker)
    {
        try
        {
            var ips = System.Net.Dns.GetHostAddresses("host.docker.internal");
            cdpUrl = $"http://{ips[0]}:9222"; 
        }
        catch { cdpUrl = "http://host.docker.internal:9222"; }
    }
    else
    {
        cdpUrl = "http://localhost:9222";
    }
}

string outputDir = isRunningInDocker ? "out" : ".";
if (isRunningInDocker && !Directory.Exists(outputDir))
{
    Directory.CreateDirectory(outputDir);
}

string outputFile = Path.Combine(outputDir, "products_data.json");

Console.WriteLine($"Environment: {(isRunningInDocker ? "Docker Container" : "Local Machine")} ");
Console.WriteLine($"Connecting to Chrome CDP at: {cdpUrl}");

using var playwright = await Playwright.CreateAsync();

IBrowser browser;
try
{
    browser = await playwright.Chromium.ConnectOverCDPAsync(cdpUrl);
}
catch (Exception)
{
    Console.WriteLine("❌ ERROR: Failed to connect to Chrome via CDP.");
    Console.WriteLine("Make sure you launched Chrome using:");
    Console.WriteLine("chrome.exe --remote-debugging-port=9222 --remote-debugging-address=0.0.0.0 --remote-allow-origins=\"*\" --user-data-dir=\"C:\\ChromeDebug\"");
    return;
}

var context = browser.Contexts[0];
var page = await context.NewPageAsync();
var parser = new BhParser(); 

var allProductsData = new List<ProductData>();
var jsonOptions = new JsonSerializerOptions { WriteIndented = true };

foreach (var url in urls)
{
    Console.WriteLine($"\n========================================");
    Console.WriteLine($"Processing product: {url}");
    
    // 1. LOAD PRODUCT PAGE
    await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    
    Console.WriteLine("⏳ Waiting for product page to load (Solve CAPTCHA if it appears)...");
    try 
    { 
        await page.WaitForSelectorAsync("h1[data-selenium='productTitle']", new PageWaitForSelectorOptions { Timeout = 120000 }); 
    }
    catch 
    { 
        Console.WriteLine("⚠️ Timeout exceeded. Product didn't load."); 
        continue; 
    }

    await Task.Delay(3000);

    // 2. EXTRACT BASIC PRODUCT INFO
    var productInfo = parser.ParseProductInfoJsonLd(await page.ContentAsync(), url);
    if (productInfo == null || string.IsNullOrEmpty(productInfo.BhNumber))
    {
        Console.WriteLine("⚠️ Failed to parse basic info. Skipping...");
        continue;
    }

    Console.WriteLine($"✅ Found: {productInfo.FullName}");
    Console.WriteLine($"Price: {productInfo.Price} | Total Reviews: {productInfo.ReviewCount}");

    // 3. NAVIGATE TO REVIEWS AND EXTRACT
    var reviews = new List<Review>();
    if (productInfo.ReviewCount > 0)
    {
        Console.WriteLine("Looking for the reviews link...");
        var reviewsLink = page.Locator("a:has(span[data-selenium='reviewsNumber'])").First;

        if (await reviewsLink.IsVisibleAsync())
        {
            Console.WriteLine("Clicking on the reviews link (JS evaluation)...");
            await reviewsLink.EvaluateAsync("el => el.click()");
            
            Console.WriteLine("⏳ Waiting for the reviews list to render...");
            try
            {
                await page.WaitForSelectorAsync("div[data-selenium='reviewsClientReview']", new PageWaitForSelectorOptions { Timeout = 60000 });
                Console.WriteLine("Reviews rendered successfully!");
                
                await Task.Delay(3000);

                Console.WriteLine("Looking for the 'Load More' button...");
                int maxClicks = 5; // Limit pagination clicks to avoid scraping thousands of reviews
                for (int i = 0; i < maxClicks; i++)
                {
                    var loadMoreBtn = page.Locator("button:has-text('Load More'), button:has-text('Show More'), button:has-text('Load more')").First;

                    if (await loadMoreBtn.IsVisibleAsync())
                    {
                        Console.WriteLine($"Loading more reviews {i + 1}/{maxClicks}...");
                        await loadMoreBtn.EvaluateAsync("el => el.click()");
                        await Task.Delay(Random.Shared.Next(3000, 5000)); 
                    }
                    else
                    {
                break; 
                    }
                }

                string reviewsHtml = await page.ContentAsync();
                reviews = parser.ParseReviewsFromHtml(reviewsHtml);
                Console.WriteLine($"✅ Extracted reviews: {reviews.Count}");
            }
            catch (TimeoutException)
            {
                Console.WriteLine("⚠️ Reviews did not appear on the screen.");
            }
        }
        else
        {
            Console.WriteLine("⚠️ Reviews link not found on the page.");
        }
    }

    // 4. COMBINE AND SAVE ITERATIVELY
    allProductsData.Add(new ProductData(productInfo, reviews));
    
    // Save to disk after every successful product to prevent data loss on crash
    await File.WriteAllTextAsync(outputFile, JsonSerializer.Serialize(allProductsData, jsonOptions));
    Console.WriteLine($"💾 Data appended and saved to '{outputFile}'");

    // Random delay before the next product to mimic human behavior
    int nextProductDelay = Random.Shared.Next(5000, 10000);
    Console.WriteLine($"Waiting {nextProductDelay / 1000} sec before the next product...");
    await Task.Delay(nextProductDelay);
}

await page.CloseAsync();
Console.WriteLine("\n🎉 Scraping finished successfully. Tab closed.");
