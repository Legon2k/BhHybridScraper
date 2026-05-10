using System;
using Xunit;
using System.IO;

public class BhParserTests
{
    [Fact]
    public void ParseProductInfoJsonLd_ReturnsExpectedProductInfo()
    {
        var html = File.ReadAllText("tests/fixtures/product_jsonld.html");
        var parser = new BhParser();

        var info = parser.ParseProductInfoJsonLd(html, "https://example.com/product/1");

        Assert.NotNull(info);
        Assert.Equal("Example Product", info!.FullName);
        Assert.Equal("12345", info.BhNumber);
        Assert.Equal(10, info.ReviewCount);
        Assert.Equal(4.5, info.Rating);
    }

    [Fact]
    public void ParseReviewsFromHtml_ReturnsReviewsList()
    {
        var html = File.ReadAllText("tests/fixtures/reviews.html");
        var parser = new BhParser();

        var reviews = parser.ParseReviewsFromHtml(html);

        Assert.NotNull(reviews);
        Assert.True(reviews.Count >= 1);
        Assert.All(reviews, r => Assert.False(string.IsNullOrEmpty(r.Id)));
    }
}
