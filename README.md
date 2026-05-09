# 🛒 B&H Photo Video Scraper & AI Analyzer (Dockerized)

A robust, enterprise-grade C# (.NET 8/10) console application designed to scrape product details and customer reviews from B&H Photo Video. 

This project was built to collect high-quality, structured review data to feed into Large Language Models (LLMs) for AI-driven sentiment analysis (e.g., generating "Pros, Cons, and Final Verdict" summaries).

## ✨ Key Features

* **Split Architecture (Docker + Host):** The scraper runs inside an isolated Docker container but connects to a live Google Chrome instance on the Host OS via CDP (Chrome DevTools Protocol).
* **Advanced Anti-Bot Bypass:** Completely defeats strict anti-scraping systems (like PerimeterX/DataDome). By using a real, human-driven Chrome profile on the host machine, we avoid the CAPTCHA loops that trap standard headless browsers.
* **Smart DNS Resolution Hack:** Automatically resolves `host.docker.internal` to a bare IP address to bypass Chrome's strict Host header security checks (avoiding the infamous `HTTP 500` DevTools error).
* **SEO JSON-LD Parsing:** Uses `HtmlAgilityPack` to extract core product data directly from hidden `application/ld+json` scripts, making the scraper highly resilient to visual UI changes.
* **Dynamic React Content Extraction:** Automatically handles pagination by locating and triggering "Load More" buttons via JavaScript evaluation to aggregate reviews without triggering mouse-movement heuristics.
* **AI-Ready Output:** Exports data into clean, minified JSON files, mapped directly to the Host OS via Docker Volumes.

## 🛠️ Tech Stack

* **C# / .NET**
* **Docker**
* **[Microsoft.Playwright](https://playwright.dev/dotnet/)** - For DOM interaction and CDP connection.
* **[HtmlAgilityPack](https://html-agility-pack.net/)** - For lightning-fast HTML parsing and XPath querying.

## 🚀 Getting Started

### 1. Prerequisites
* Install[Docker Desktop](https://www.docker.com/products/docker-desktop/).
* Ensure Google Chrome is installed on your host machine.

### 2. The Magic Step: Launch Chrome in Debug Mode (On Host OS)
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
Run the container, map the host network, and mount an output volume so the JSON files are saved to your local machine:

**For Windows (PowerShell) / macOS / Linux:**
```powershell
docker run --rm --add-host=host.docker.internal:host-gateway -v "${PWD}/out:/app/out" bh-scraper reviews
```

**For Windows (Command Prompt - cmd):**
```cmd
docker run --rm --add-host=host.docker.internal:host-gateway -v "%cd%\out:/app/out" bh-scraper reviews
```

### Execution Modes
You can change the last word of the docker command to switch modes:
* `reviews` (Default) - Scrapes products and deeply extracts user feedback. Out