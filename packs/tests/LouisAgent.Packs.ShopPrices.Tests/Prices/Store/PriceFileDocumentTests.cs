using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Store;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices.Store;

/// <summary>A table's document is keyed by its date and source, so each month is kept once, and gives the table back.</summary>
public class PriceFileDocumentTests
{
    /// <summary>The id is the date then the source; the table comes back with the same fields.</summary>
    [Test]
    public void ItsId_IsTheDateAndTheSource_AndItGivesTheTableBack()
    {
        var statsSa = PriceBook.Default.Files[0];

        var document = PriceFileDocument.Of(statsSa);
        var back = document.ToFile();

        Assert.That(document.Id, Is.EqualTo($"{statsSa.AsOf:yyyy-MM-dd} {statsSa.Source}"));
        Assert.That(document.Id, Does.StartWith("2026-08-01 "));
        Assert.That((back.Source, back.AsOf, back.Products), Is.EqualTo((statsSa.Source, statsSa.AsOf, statsSa.Products)));
    }
}
