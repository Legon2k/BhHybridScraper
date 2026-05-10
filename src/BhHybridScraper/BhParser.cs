using System;
using System.Collections.Generic;
using System.Text.Json;
using HtmlAgilityPack;

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
            catch { /* Ignore parsing errors from unrelated JSON blocks */ }
        }
        return null;
    }
    public List<Review> ParseReviewsFromHtml(string html)    {
        var reviews = new List<Review>();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var reviewNodes = doc.DocumentNode.SelectNodes("//div[@data-selenium='reviewsClientReview']");        if (reviewNodes == null) return reviews;

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
                var starsNode = node.SelectNodes(".//*[@data-selenium='ratingContainer']//*[local-name()='svg']");                int rating = starsNode?.Count ?? 5; 

                reviews.Add(new Review(
                    Id: Guid.NewGuid().ToString("N").Substring(0, 8),
                    Author: author,
                    Rating: rating,
                    Text: fullText,
                    Date: date
                ));
            }
            catch { /* Skip broken review blocks */ }
        }
        return reviews;    }
}
