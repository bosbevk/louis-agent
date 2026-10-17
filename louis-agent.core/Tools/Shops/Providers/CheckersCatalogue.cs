namespace LouisAgent.Core.Tools.Shops.Providers;

using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Tools.Shops;
using LouisAgent.Core.Tools.Web.Browsing;

/// <summary>
/// Checkers' products from its own catalogue (F33 step 2). Its search page, at the address its own search box sends
/// (<c>/search?Search=…</c>; <c>/search?q=</c> finds nothing), fetches <c>/api/catalogue/get-products-filter</c>, whose
/// answer gives each product's name, price, unit and article number; the page's cards link each product as
/// <c>/product/{name}-{article}{unit}</c>. The page is visited through the guarded browser like any shopper's, its
/// catalogue answer kept (no internal API called directly), and parsed here. For development and benchmarks until
/// Checkers' terms are checked (ADR 0023). Where the user shared their town, the search runs in the store that serves it
/// (F34: <see cref="CheckersStore"/>), else in Checkers' default store; the answer says which.
/// </summary>
public sealed partial class CheckersCatalogue(IWebBrowser browser, CheckersStore? store = null) : IShopCatalogue
{
    private static readonly Uri Site = new("https://www.checkers.co.za/");
    private const string CataloguePath = "/api/catalogue/get-products-filter";
    // Where the search page's cards load a product's photo from, by the answer's imageId (seen live, 2026-10-17).
    private static readonly Uri Images = new("https://catalog.sixty60.co.za/v2/files/");

    public ShopKind Shop => ShopKind.Checkers;

    public async Task<ShopSearch> SearchAsync(string query, int maxResults, CancellationToken cancellationToken = default)
    {
        var search = SearchUri(query);
        string? place = UserPlace.Current?.Location;
        BrowserSession? area = store is null ? null : await store.ForAsync(place, cancellationToken);
        BrowserVisit visit;
        try
        {
            visit = await browser.VisitAsync(search, url => url.AbsolutePath == CataloguePath, area, cancellationToken: cancellationToken);
        }
        catch (WebBrowserException ex)
        {
            throw new ShopCatalogueException(ex.Message);
        }

        if (visit.Captured.Count == 0)
            throw new ShopCatalogueException(MessageText.Format(ToolMessage.ShopCatalogueNoAnswer, new { Shop = "Checkers", Query = query, Url = search }));
        try
        {
            return new ShopSearch(Parse(visit.Captured[0], visit.Html, search, maxResults), area is null ? null : place!.Trim());
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            throw new ShopCatalogueException(MessageText.Format(ToolMessage.ShopCatalogueUnreadable, new { Shop = "Checkers", Query = query, Reason = ex.Message }));
        }
    }

    /// <summary>The search, as Checkers' own search box sends it.</summary>
    internal static Uri SearchUri(string query) => new(Site, "search?Search=" + Uri.EscapeDataString(query.Trim()));

    /// <summary>
    /// The catalogue's answer as products: in stock only (one that can't be bought isn't a price), at what it costs now
    /// (a special's price when lower), each or per kg, each linked to its card's page, or to the search when the page
    /// has no card for it. Checkers prices what's sold by weight per kg; a pack's price is that times its weight.
    /// </summary>
    internal static IReadOnlyList<ShopProduct> Parse(string json, string html, Uri searchUri, int maxResults)
    {
        using var doc = JsonDocument.Parse(json);
        var products = new List<ShopProduct>();
        foreach (var p in doc.RootElement.GetProperty("products").EnumerateArray())
        {
            if (p.TryGetProperty("isStockAvailable", out var stock) && stock.ValueKind == JsonValueKind.False) continue;
            if (!p.TryGetProperty("price", out var priceValue) || priceValue.ValueKind != JsonValueKind.Number) continue;

            decimal price = priceValue.GetDecimal();
            if (p.TryGetProperty("discountedPrice", out var discounted) && discounted.ValueKind == JsonValueKind.Number
                && discounted.GetDecimal() is > 0 and var now && now < price)
                price = now;

            string unit = p.TryGetProperty("unitOfMeasure", out var u) ? u.GetString() ?? "" : "";
            bool byWeight = unit.Equals("KG", StringComparison.OrdinalIgnoreCase);
            string article = p.TryGetProperty("articleNumber", out var a) ? a.GetString() ?? "" : "";
            var weight = byWeight ? Weight(p) : null;

            products.Add(new ShopProduct(
                p.GetProperty("name").GetString() ?? "",
                byWeight ? Pack(price, weight?.MinKg) : price,
                byWeight && weight is { } w && w.MaxKg != w.MinKg ? Pack(price, w.MaxKg) : null,
                p.TryGetProperty("currency", out var c) ? c.GetString() ?? "ZAR" : "ZAR",
                weight is { } kg ? Kg(kg.MinKg, kg.MaxKg) : null,
                byWeight ? price : null,
                byWeight ? "kg" : "each",
                LinkOf(html, article, unit) ?? searchUri,
                ImageOf(p)));
            if (products.Count == maxResults) break;
        }
        return products;
    }

    /// <summary>A product sold by weight comes in packs of a range of grams ("1100-1300"): 1.1 to 1.3 kg.</summary>
    private static (decimal MinKg, decimal MaxKg)? Weight(JsonElement p)
    {
        if (!p.TryGetProperty("weightOptions", out var options) || options.ValueKind != JsonValueKind.Array || options.GetArrayLength() == 0)
            return null;
        decimal[] kg = (options[0].GetString() ?? "").Split('-', StringSplitOptions.TrimEntries)
            .Select(g => decimal.Parse(g, CultureInfo.InvariantCulture) / 1000m).ToArray();
        return (kg[0], kg[^1]);
    }

    /// <summary>"1.1–1.3 kg", or "0.5 kg" for packs of one weight.</summary>
    private static string Kg(decimal min, decimal max)
    {
        static string Of(decimal kg) => kg.ToString("0.###", CultureInfo.InvariantCulture);
        return min == max ? $"{Of(min)} kg" : $"{Of(min)}–{Of(max)} kg";
    }

    /// <summary>What a pack of this weight costs at the price per kg, to the cent; no weight, no pack price.</summary>
    private static decimal? Pack(decimal perKg, decimal? kg) =>
        kg is { } weight ? Math.Round(perKg * weight, 2, MidpointRounding.AwayFromZero) : null;

    /// <summary>The product's photo, as its card shows it (256 pixels), or none when the answer names none.</summary>
    private static Uri? ImageOf(JsonElement p) =>
        p.TryGetProperty("imageId", out var id) && id.ValueKind == JsonValueKind.String && id.GetString() is { Length: > 0 } image
            ? new Uri(Images, Uri.EscapeDataString(image) + "?width=256&height=256")
            : null;

    /// <summary>The product's card on the search page: a link ending in its article number and unit.</summary>
    private static Uri? LinkOf(string html, string article, string unit)
    {
        if (article.Length == 0) return null;
        var link = Regex.Match(html, "href=\"(/product/[^\"]*-" + Regex.Escape(article) + Regex.Escape(unit) + ")\"", RegexOptions.IgnoreCase);
        return link.Success ? new Uri(Site, link.Groups[1].Value) : null;
    }
}
