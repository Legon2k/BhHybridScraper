# 🛒 B&H Photo Video Scraper & AI Analyzer (Dockerized)

> NOTE: The project has been renamed and reorganized. The main project is now `BhHybridScraper.Core` and lives in `src/BhHybridScraper.Core/`. The Dockerfile and build scripts were updated to reference `BhHybridScraper.Core` and the container entrypoint `BhHybridScraper.Core.dll`.

A robust, enterprise-grade C# (.NET 10) console application designed to scrape product details and customer reviews from B&H Photo Video in a single, unified pipeline. 

This project was built to collect high-quality, structured review data to feed into Large Language Models (LLMs) for AI-driven sentiment analysis (e.g., generating "Pros, Cons, and Final Verdict" summaries).

## ✨ Key Features

* **Unified Data Pipeline:** Seamlessly visits a product page, extracts core specifications via JSON-LD, navigates to the reviews section, paginates through feedback, and combines everything into a single AI-ready `ProductData` object.
* **Iterative Saving:** Protects against data loss during long scraping sessions by saving the JSON dataset to disk after every successful product extraction.
* **Split Architecture (Docker + Host):** The scraper runs inside an isolated Docker container but connects to a live Google Chrome instance on the Host OS via CDP (Chrome DevTools Protocol).
* **Advanced Anti-Bot Bypass:** Completely defeats strict anti-scraping systems (like PerimeterX/DataDome). By using a real, human-driven Chrome profile on the host machine, we avoid the CAPTCHA loops that trap standard headless browsers.
* **SEO JSON-LD Parsing:** Uses `HtmlAgilityPack` to extract core product data directly from hidden `application/ld+json` scripts, making the scraper highly resilient to visual UI changes.

## 🛠️ Tech Stack

* **C# / .NET 10**
* **Docker**
* **[Microsoft.Playwright](https://playwright.dev/dotnet/)** - For DOM interaction and CDP connection.
* **[HtmlAgilityPack](https://html-agility-pack.net/)** - For lightning-fast HTML parsing and XPath querying.

## 🚀 Getting Started

### 1. Prerequisites
* Install [Docker Desktop](https://www.docker.com/products/docker-desktop/).
* Ensure Google Chrome is installed on your host machine.

### 2. Launch Chrome in Debug Mode (On Host OS)
Because B&H uses aggressive anti-bot protection, we must run the browser on the host machine and open a debugging port for Docker.

**Close ALL existing Chrome windows**, open your terminal (Command Prompt / PowerShell), and run:

**For Windows:**
```cmd
chrome.exe --remote-debugging-port=9222 --remote-debugging-address=0.0.0.0 --remote-allow-origins="*" --user-data-dir="C:\ChromeDebug"
```
**For macOS:**
```bash
/Applications/Google\ Chrome.app/Contents/MacOS/Google\ Chrome --remote-debugging-port=9222 --remote-debugging-address=0.0.0.0 --remote-allow-origins="*" --user-data-dir="~/ChromeDebug"
```
*Note: Keep this browser window open. If prompted by Windows Defender Firewall, click **Allow**.*

### 3. Build the Docker Image
Open a new terminal in the project folder and build the scraper:
```bash
docker build -t bh-scraper .
```

### 4. Run the Scraper via Docker
Run the container, map the host network, and mount an output volume so the combined `products_data.json` file is saved directly to your local machine:

**For Windows (PowerShell) / macOS / Linux:**
```powershell
docker run --rm --add-host=host.docker.internal:host-gateway -v "${PWD}/out:/app/out" bh-scraper
```

**For Windows (Command Prompt - cmd):**
```cmd
docker run --rm --add-host=host.docker.internal:host-gateway -v "%cd%\out:/app/out" bh-scraper
```

*Note: If you want to run Chrome on a remote machine in your local network, you can pass the IP address via an environment variable:*
```bash
docker run --rm -e CDP_URL="http://192.168.1.15:9222" -v "${PWD}/out:/app/out" bh-scraper
```

## 🔮 Future Roadmap (AI Integration)

- [ ] **OpenAI Integration:** Implement `Microsoft.Extensions.AI` to send the generated `products_data.json` directly to ChatGPT/Claude to generate a "Should I Buy This?" summary.
- [ ] **SQLite Migration:** Move from JSON files to SQLite using Entity Framework Core for advanced review filtering prior to AI ingestion.

## ⚠️ Disclaimer

This project is intended for **educational purposes only**. Web scraping may violate the Terms of Service of some websites. Always scrape responsibly and avoid overloading servers with aggressive request rates.

## ✅ Unit tests

A test project `tests\BhHybridScraper.Core.UnitTests` was added to validate parsing logic (uses xUnit). Key details:

- Test project target: `.NET 10` (same TFM as the main code).
- Fixtures (HTML samples) are stored in `tests/fixtures` and are configured to be copied to the test output so tests can read them via the relative path `tests/fixtures/...` at runtime.
- Tests exercise `BhParser` behavior: `ParseProductInfoJsonLd` and `ParseReviewsFromHtml`.

How to run tests:

- From the command line (PowerShell):
  - `dotnet test tests\BhHybridScraper.Core.UnitTests\BhHybridScraper.Core.UnitTests.csproj`
- From Visual Studio: open Test Explorer and run the tests for `BhHybridScraper.Core.UnitTests`.

If a test reports a missing fixture, ensure the project has been rebuilt so the fixtures are copied to the test output directory (rebuild or run `dotnet build` before `dotnet test`).
