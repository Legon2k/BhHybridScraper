public record ProductInfo(string ProductUrl, string ShortName, string FullName, string BhNumber, string MfrNumber, string ImageUrl, string Price, int ReviewCount, double Rating);
public record Review(string Id, string Author, int Rating, string Text, string Date);
public record ProductData(ProductInfo Info, System.Collections.Generic.List<Review> Reviews);
