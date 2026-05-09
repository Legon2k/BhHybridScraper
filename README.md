# 🛒 B&H Photo Video Scraper & AI Analyzer

A robust C# (.NET 8) console application designed to scrape product details and customer reviews from B&H Photo Video. 

This project was built with a specific goal: **to collect high-quality, structured review data to feed into Large Language Models (LLMs) for AI-driven sentiment analysis** (e.g., generating "Pros, Cons, and Final Verdict" summaries).

## ✨ Key Features

* **Advanced Anti-Bot Bypass:** Defeats strict anti-scraping systems (like PerimeterX/DataDome) by using **Playwright** to connect to a live, human-driven Google Chrome instance via CDP (Chrome DevTools Protocol). No CAPTCHA loops!
* **SEO JSON-LD Parsing:** Uses `HtmlAgilityPack` to extract core product data directly from hidden `application/ld+json` scripts. This makes the scraper highly resilient to visual UI changes.
* **Dynamic Content Extraction:** Automatically handles React-based pagination by locating and clicking "Load More" buttons to aggregate reviews.
* **Dual Execution Modes:** Run the scraper in `info` mode (basic specs & prices) or `reviews` mode (deep scraping of user feedback).
* **AI-Ready Output:** Exports data into clean, minified JSON files, perfectly structured for prompt engineering and OpenAI/Claude APIs.

## 🛠️ Tech Stack

* **C# 12 / .NET 8.0**
* **[Microsoft.Playwright](https://playwright.dev/dotnet/)** - For browser automation, DOM interaction, and CDP connection.
* **[HtmlAgilityPack](https://html-agility-pack.net/)** - For lightning-fast HTML parsing and XPath querying.
* **System.Text.Json** - For high-performance JSON serialization/deserialization.

## 🚀 Getting Started

### 1. Prerequisites
* Install [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
* Ensure Google Chrome is installed on your machine.

### 2. Setup
Clone the repository and install the required dependencies:
```bash
git clone https://github.com/yourusername/bh-scraper.git
cd bh-scraper
dotnet restore
dotnet build
```

### 3. The Magic Step: Launch Chrome in Debug Mode
Because B&H uses aggressive anti-bot protection, standard headless browsers will be blocked (403 Forbidden). We bypass this by connecting the script to a real, isolated Chrome profile.

**Close all existing Chrome windows**, open your terminal (Command Prompt / PowerShell), and run:
```cmd
# For Windows
chrome.exe --remote-debugging-port=9222 --user-data-dir="C:\ChromeDebug"

# For macOS
/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --remote-debugging-port=9222 --user-data-dir="~/ChromeDebug"
```
*Note: Keep this browser window open. The script will attach to it automatically.*

### 4. Run the Scraper
Open a new terminal window in the project folder and run the app. You can pass arguments to choose the scraping mode:

**To scrape basic product info only:**
```bash
dotnet run -- info
```
*(Outputs to `products_info.json`)*

**To scrape products and their reviews (Default):**
```bash
dotnet run -- reviews
```
*(Outputs to `products_reviews.json`)*

## 🏗️ Project Architecture

This project uses a hybrid scraping approach to maximize both stealth and performance:
1. **Transport Layer (Playwright):** Controls a live Chrome instance. Navigates pages, triggers JS events, and handles "Load More" buttons seamlessly.
2. **Parsing Layer (HtmlAgilityPack):** Playwright extracts the raw HTML string and passes it to HAP. HAP uses precise XPath queries and JSON-LD deserialization to parse the data in milliseconds without consuming heavy browser resources.

## 🔮 Future Roadmap (AI Integration)

- [ ] **SQLite Migration:** Move from JSON files to SQLite using Entity Framework Core for better data filtering (e.g., fetching only 1-star and 5-star reviews).
- [ ] **OpenAI Integration:** Implement `Microsoft.Extensions.AI` to send scraped reviews directly to ChatGPT/Claude to generate a "Should I Buy This?" summary.
- [ ] **Data Cleaning:** Pre-process review texts to remove HTML entities and optimize token usage before sending them to the LLM context window.

## ⚠️ Disclaimer

This project is intended for **educational purposes only**. Web scraping may violate the Terms of Service of some websites. Always scrape responsibly, respect `robots.txt`, and avoid overloading servers with aggressive request rates. The authors are not responsible for any misuse of this software.