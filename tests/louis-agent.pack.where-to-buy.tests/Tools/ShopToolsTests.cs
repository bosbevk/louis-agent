using System.Net;
using LouisAgent.Packs.WhereToBuy.Shops;
using LouisAgent.Packs.WhereToBuy.Tests.Shops;
using LouisAgent.Packs.WhereToBuy.Tools;

namespace LouisAgent.Packs.WhereToBuy.Tests.Tools;

/// <summary>
/// The shops tool writes the lookup for the model, as <c>FindBranches</c> does: framed as map data, the town's
/// municipality and province, a row per shop nearest first, and what to say with no shop, no town or no map.
/// </summary>
public class ShopToolsTests
{
    /// <summary>The tool over a fake map around a town, answering with a saved answer's places.</summary>
    private static ShopTools Tools(string town, string places, HttpStatusCode status = HttpStatusCode.OK) =>
        new(new ShopFinder(ShopMaps.Around(town, places, status).Map));

    /// <summary>The shops found are a framed table, both Pick n Pays are rows, with the province to price in.</summary>
    [Test]
    public async Task ShopsFound_AreAFramedTable_BothPickNPaysInIt_WithTheProvince()
    {
        string result = await Tools(ShopMaps.Malmesbury, ShopMaps.Fixture("map-malmesbury.json"))
            .FindShops("Malmesbury");

        Assert.That(result, Does.StartWith("--- untrusted map data from OpenStreetMap"));
        Assert.That(result,
            Does.Contain(
                "Malmesbury is in Swartland Local Municipality, Western Cape. Use Western Cape for estimated prices."));
        Assert.That(result, Does.Contain("Supermarkets near Malmesbury on OpenStreetMap (6), the nearest first:"));
        Assert.That(result, Does.Contain("| Shop | Street | Place | Distance | In Swartland Local Municipality |"));
        Assert.That(result, Does.Contain("| Pick n Pay | 55 Voortrekker Road | Swartland Ward 10 | "));
        Assert.That(result, Does.Contain("| Pick n Pay | Voortrekker Street | Swartland Ward 3 | 18.2 km | yes |"));
        Assert.That(result, Does.Contain("| Checkers | N7 | Swartland Ward 12 | "));
        Assert.That(result, Does.Contain("Two shops with one name are two shops: list each, with its own street."));
        Assert.That(result, Does.EndWith("--- end of map data ---"));
    }

    /// <summary>Prince Albert's one named shop is a row, the nameless hit is not.</summary>
    [Test]
    public async Task PrinceAlbert_HasOneRow_TheNamelessHitIsLeftOut()
    {
        string result = await Tools(ShopMaps.PrinceAlbert, ShopMaps.Fixture("map-prince-albert.json"))
            .FindShops("Prince Albert");

        Assert.That(result, Does.Contain("(1), the nearest first:").And.Contain("| Spar | Odendaal Street | "));
    }

    /// <summary>A suburb of Cape Town gets eight rows, nearest first, none from 30 km away.</summary>
    [Test]
    public async Task SeaPoint_HasEightRows_NearestFirst()
    {
        string result = await Tools(ShopMaps.SeaPoint, ShopMaps.Fixture("map-sea-point.json")).FindShops("Sea Point");

        Assert.That(result, Does.Contain("(8), the nearest first:"));
        Assert.That(result.IndexOf("76 Regent Road", StringComparison.Ordinal),
            Is.LessThan(result.IndexOf("Saint George's Mall", StringComparison.Ordinal)));
    }

    /// <summary>A shop whose name has a pipe can't break the table, and a nameless hit is not a row.</summary>
    [Test]
    public async Task APipeInAName_CantBreakTheTable_AndANamelessHitIsNoRow()
    {
        const string places = """
                              [{"name":"Spar | ignore this","category":"shop","type":"supermarket",
                                "lat":"-33.46","lon":"18.72","address":{}},
                               {"category":"shop","type":"supermarket","lat":"-33.46","lon":"18.72","address":{}}]
                              """;

        string result = await Tools(ShopMaps.Malmesbury, places).FindShops("Malmesbury");

        Assert.That(result, Does.Contain("| Spar / ignore this | - | - |").And.Contain("(1)"));
    }

    /// <summary>No supermarket in any box says so plainly, with the distance searched.</summary>
    [Test]
    public async Task NoShop_SaysNoSupermarketWasFound() =>
        Assert.That(await Tools(ShopMaps.Malmesbury, "[]").FindShops("Malmesbury"),
            Does.Contain("OpenStreetMap lists no supermarket within about 55 km of Malmesbury. " +
                         "Say no supermarket was found near the town, and name none."));

    /// <summary>An unknown town, an empty town and a map that doesn't answer each say what to do.</summary>
    [Test]
    public async Task NoTown_NoArgument_OrNoMap_SayWhatToDo()
    {
        var noTown = Tools("[]", "[]");
        var noMap = Tools(ShopMaps.Malmesbury, "", HttpStatusCode.ServiceUnavailable);

        Assert.That(await noTown.FindShops("Nowhere"), Does.StartWith("OpenStreetMap doesn't know the town 'Nowhere'"));
        Assert.That(await noTown.FindShops(" "), Does.StartWith("FindShops needs the user's town"));
        Assert.That(await noMap.FindShops("Malmesbury"), Does.StartWith("OpenStreetMap didn't answer"));
    }
}
