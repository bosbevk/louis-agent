using LouisAgent.Packs.ShopPrices.Prices;
using LouisAgent.Packs.ShopPrices.Prices.Regions;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices.Regions;

/// <summary>A province is read the way people write it, in English or Afrikaans.</summary>
public class RegionNamesTests
{
    /// <summary>The usual spellings each name their province; nothing at all is the country.</summary>
    [TestCase("Western Cape", Region.WesternCape)]
    [TestCase("western-cape", Region.WesternCape)]
    [TestCase(" Wes-Kaap ", Region.WesternCape)]
    [TestCase("KwaZulu-Natal", Region.KwaZuluNatal)]
    [TestCase("KZN", Region.KwaZuluNatal)]
    [TestCase("Kwa-Zulu Natal", Region.KwaZuluNatal)]
    [TestCase("Free State", Region.FreeState)]
    [TestCase("Vrystaat", Region.FreeState)]
    [TestCase("North West", Region.NorthWest)]
    [TestCase("gauteng", Region.Gauteng)]
    [TestCase(null, Region.National)]
    [TestCase("", Region.National)]
    [TestCase("South Africa", Region.National)]
    public void AProvince_IsReadAsItsRegion(string? name, Region region)
    {
        Assert.That(RegionNames.TryParse(name, out var read), Is.True);
        Assert.That(read, Is.EqualTo(region));
    }

    /// <summary>A name that is no province is refused, not quietly taken as the country.</summary>
    [TestCase("Stellenbosch")]
    [TestCase("Cape")]
    public void APlaceThatIsNoProvince_IsRefused(string name) =>
        Assert.That(RegionNames.TryParse(name, out _), Is.False);
}
