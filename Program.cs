using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using HtmlAgilityPack;

Console.OutputEncoding = System.Text.Encoding.UTF8;
string mode = args.Length > 0 ? args[0].ToLower() : "reviews";

var urls = new List<string>
{
    "https://www.bhphotovideo.com/c/product/1955563-REG/samsung_sm_x230nzatxar_11_galaxy_tab_a11.html",
    // "https://www.bhphotovideo.com/c/product/1850005-REG/lenovo_zadg0016us_11_tab_m11_multi_touch.html"
};

Console.WriteLine($"Режим: [{mode.ToUpper()}]. Подключаемся к вашему живому Google Chrome...");

using var playwright = await Playwright.CreateAsync();

// =====================================================================
// ГЛАВНАЯ МАГИЯ: Подключаемся к уже открытому реальному Chrome!
// (Убедитесь, что запустили chrome.exe --remote-debugging-port=9222)
// =====================================================================
IBrowser browser;
try
{
    browser = await playwright.Chromium.ConnectOverCDPAsync("http://localhost:9222");
}
catch (Exception)
{
    Console.WriteLine("❌ ОШИБКА: Не удалось подключиться к Chrome.");
    Console.WriteLine("Убедитесь, что вы закрыли все окна Chrome и запустили его командой:");
    Console.WriteLine("chrome.exe --remote-debugging-port=9222");
    return;
}

// Берем первую открытую вкладку или создаем новую
var context = browser.Contexts[0];
var page = await context.NewPageAsync();
var parser = new BhParser(); 

var scrapedInfo = new List<ProductInfo>();
var scrapedReviews = new Dictionary<string, List<Review>>(); 

foreach (var url in urls)
{
    Console.WriteLine($"\n========================================");
    Console.WriteLine($"Обработка товара: {url}");
    
    await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
    
    try { await page.WaitForSelectorAsync("h1[data-selenium='productTitle']", new PageWaitForSelectorOptions { Timeout = 60000 }); }
    catch { Console.WriteLine("⚠️ Товар не загрузился (или вы не решили капчу)."); continue; }

    await Task.Delay(3000); 

    if (mode == "info")
    {
        var productInfo = parser.ParseProductInfoJsonLd(await page.ContentAsync(), url);
        if (productInfo != null && !string.IsNullOrEmpty(productInfo.BhNumber))
        {
            Console.WriteLine($"✅ Найден: {productInfo.FullName} | Цена: {productInfo.Price}");
            scrapedInfo.Add(productInfo);
        }
    }
    else if (mode == "reviews")
    {
        var productInfo = parser.ParseProductInfoJsonLd(await page.ContentAsync(), url);
        if (productInfo == null) continue;

        Console.WriteLine("Ищем ссылку на отзывы...");
        var reviewsLink = page.Locator("a:has(span[data-selenium='reviewsNumber'])").First;

        if (await reviewsLink.IsVisibleAsync())
        {
            Console.WriteLine("Открываем отзывы (JS-клик)...");
            await reviewsLink.EvaluateAsync("el => el.click()");
            
            Console.WriteLine("⏳ Ожидаем список отзывов...");
            try
            {
                await page.WaitForSelectorAsync("div[data-selenium='reviewsClientReview']", new PageWaitForSelectorOptions { Timeout = 30000 });
                Console.WriteLine("Отзывы на экране!");
            }
            catch (TimeoutException)
            {
                Console.WriteLine("⚠️ Отзывы так и не появились.");
                continue; 
            }

            await Task.Delay(3000); 

            Console.WriteLine("Ищем кнопку 'Load More'...");
            int maxClicks = 5; 
            for (int i = 0; i < maxClicks; i++)
            {
                var loadMoreBtn = page.Locator("button:has-text('Load More'), button:has-text('Show More'), button:has-text('Load more')").First;

                if (await loadMoreBtn.IsVisibleAsync())
                {
                    Console.WriteLine($"Подгружаем отзывы {i + 1}/{maxClicks}...");
                    await loadMoreBtn.EvaluateAsync("el => el.click()");
                    await Task.Delay(Random.Shared.Next(3000, 5000)); 
                }
                else
                {
                    break; 
                }
            }

            string reviewsHtml = await page.ContentAsync();
            var reviews = parser.ParseReviewsFromHtml(reviewsHtml);
            Console.WriteLine($"✅ Отзывов извлечено: {reviews.Count}");
            
            scrapedReviews[url] = reviews;
        }
    }

    await Task.Delay(Random.Shared.Next(3000, 6000));
}

var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
if (mode == "info")
{
    await File.WriteAllTextAsync("products_info.json", JsonSerializer.Serialize(scrapedInfo, jsonOptions));
    Console.WriteLine("\n🎉 Данные сохранены в 'products_info.json'.");
}
else if (mode == "reviews")
{
    await File.WriteAllTextAsync("products_reviews.json", JsonSerializer.Serialize(scrapedReviews, jsonOptions));
    Console.WriteLine("\n🎉 Отзывы сохранены в 'products_reviews.json'.");
}

// ВАЖНО: Мы закрываем только вкладку, которую создали, сам ваш браузер останется открытым!
await page.CloseAsync();
await browser.CloseAsync();

// ==========================================
// КЛАСС ПАРСЕРА
// ==========================================
public class BhParser
{
    public ProductInfo? ParseProductInfoJsonLd(string html, string url)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var scriptNodes = doc.DocumentNode.SelectNodes("//script[@type='application/ld+json']");
        if (scriptNodes == null) return null;

        foreach (var node in scriptNodes)
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(node.InnerText);
                var root = jsonDoc.RootElement;

                if (root.TryGetProperty("@type", out var typeProp) && typeProp.GetString() == "Product")
                {
                    string fullName = root.GetProperty("name").GetString() ?? "";
                    string shortName = fullName.Contains(" - ") ? fullName.Split(" - ")[0].Trim() : fullName;
                    string bhNumber = root.TryGetProperty("sku", out var skuProp) ? skuProp.GetString() ?? "" : "";
                    string mfrNumber = root.TryGetProperty("mpn", out var mpnProp) ? mpnProp.GetString() ?? "" : "";
                    string imageUrl = root.TryGetProperty("image", out var imgProp) ? imgProp.GetString() ?? "" : "";

                    string price = "";
                    if (root.TryGetProperty("offers", out var offers) && offers.TryGetProperty("price", out var priceProp))
                        price = "$" + priceProp.GetString();

                    int reviewCount = 0;
                    double rating = 0;
                    if (root.TryGetProperty("aggregateRating", out var aggRating))
                    {
                        if (aggRating.TryGetProperty("reviewCount", out var rc)) reviewCount = rc.GetInt32();
                        if (aggRating.TryGetProperty("ratingValue", out var rv))
                        {
                            if (rv.ValueKind == JsonValueKind.Number) rating = rv.GetDouble();
                            else if (rv.ValueKind == JsonValueKind.String) double.TryParse(rv.GetString(), System.Globalization.CultureInfo.InvariantCulture, out rating);
                        }
                    }

                    return new ProductInfo(url, shortName, fullName, bhNumber, mfrNumber, imageUrl, price, reviewCount, rating);
                }
            }
            catch { }
        }
        return null;
    }

    public List<Review> ParseReviewsFromHtml(string html)
    {
        var reviews = new List<Review>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var reviewNodes = doc.DocumentNode.SelectNodes("//div[@data-selenium='reviewsClientReview']");
        if (reviewNodes == null) return reviews;

        foreach (var node in reviewNodes)
        {
            try
            {
                string author = node.SelectSingleNode(".//*[@data-selenium='reviewsClientReviewByReviewer']//span")?.InnerText.Trim() ?? "Anonymous";
                string date = node.SelectSingleNode(".//*[@data-selenium='reviewsClientReviewDate']")?.InnerText.Trim() ?? "";
                string title = node.SelectSingleNode(".//*[@data-selenium='reviewsClientReviewTitle']")?.InnerText.Trim() ?? "";
                string content = node.SelectSingleNode(".//*[@data-selenium='reviewsClientReviewContent']")?.InnerText.Trim() ?? "";
                
                string fullText = string.IsNullOrEmpty(title) ? content : $"{title}: {content}";
                fullText = System.Net.WebUtility.HtmlDecode(fullText);

                var starsNode = node.SelectNodes(".//*[@data-selenium='ratingContainer']//*[local-name()='svg']");
                int rating = starsNode?.Count ?? 5; 

                reviews.Add(new Review(
                    Id: Guid.NewGuid().ToString("N").Substring(0, 8),
                    Author: author,
                    Rating: rating,
                    Text: fullText,
                    Date: date
                ));
            }
            catch { }
        }

        return reviews;
    }
}

public record ProductInfo(string ProductUrl, string ShortName, string FullName, string BhNumber, string MfrNumber, string ImageUrl, string Price, int ReviewCount, double Rating);
public record Review(string Id, string Author, int Rating, string Text, string Date);