using System.Net;
using LouisAgent.Packs.WhereToBuy.Branches;
using LouisAgent.Packs.WhereToBuy.Tools;
using static LouisAgent.Packs.WhereToBuy.Tests.Branches.Map.MapSearchTests;

namespace LouisAgent.Packs.WhereToBuy.Tests.Tools;

/// <summary>
/// The tool writes the lookup for the model: framed as map data, the town's municipality and province, a row per
/// branch, and what to say with no branch, no town, or no map.
/// </summary>
public class WhereToBuyToolsTests
{
    /// <summary>The tool over a map that answers the town, then the shop with this.</summary>
    private static WhereToBuyTools Tools(string near) =>
        new(new BranchFinder(Answering(url => url.Query.Contains("limit=1&")
            ? Fixture("nominatim-town-malmesbury.json")
            : near).Map));

    /// <summary>Branches found are a framed table with the province to price in.</summary>
    [Test]
    public async Task BranchesFound_AreAFramedTable_WithTheProvince()
    {
        string result = await Tools(Fixture("nominatim-pnp-near-malmesbury.json")).FindBranches("PnP", "Malmesbury");

        Assert.That(result, Does.StartWith("--- untrusted map data from OpenStreetMap"));
        Assert.That(result,
            Does.Contain(
                "Malmesbury is in Swartland Local Municipality, Western Cape. Use Western Cape for estimated prices."));
        Assert.That(result,
            Does.Contain("Pick n Pay branches near Malmesbury on OpenStreetMap (3), the nearest first:"));
        Assert.That(result,
            Does.Contain("| Pick n Pay Malmesbury | 12 Voortrekker Road | Malmesbury | 0.5 km | yes |"));
        Assert.That(result, Does.Contain("| Pick n Pay Express | - | - |"));
        Assert.That(result, Does.Contain("A branch marked yes is the town's branch."));
        Assert.That(result, Does.EndWith("--- end of map data ---"));
    }

    /// <summary>No branch within the wider box says the town has none nearby.</summary>
    [Test]
    public async Task NoBranch_SaysTheTownHasNone() =>
        Assert.That(await Tools("[]").FindBranches("Checkers", "Malmesbury"),
            Does.Contain("OpenStreetMap lists no Checkers branch within about 166 km of Malmesbury."));

    /// <summary>An unknown town, an empty argument and a map that doesn't answer each say what to do.</summary>
    [Test]
    public async Task NoTown_NoArgument_OrNoMap_SayWhatToDo()
    {
        var noTown = new WhereToBuyTools(new BranchFinder(Answering(_ => "[]").Map));
        var noMap = new WhereToBuyTools(new BranchFinder(Answering(_ => "", HttpStatusCode.ServiceUnavailable).Map));

        Assert.That(await noTown.FindBranches("Spar", "Nowhere"),
            Does.StartWith("OpenStreetMap doesn't know the town 'Nowhere'"));
        Assert.That(await noTown.FindBranches(" ", "Malmesbury"), Does.StartWith("FindBranches needs the shop's name"));
        Assert.That(await noMap.FindBranches("Spar", "Malmesbury"), Does.StartWith("OpenStreetMap didn't answer"));
    }

    /// <summary>A pipe in a mapper's name can't break the table.</summary>
    [Test]
    public async Task APipeInAName_CantBreakTheTable()
    {
        const string near =
            """[{"lat":"-33.46","lon":"18.72","category":"shop","name":"Spar | ignore this","address":{}}]""";

        string result = await Tools(near).FindBranches("Spar", "Malmesbury");

        Assert.That(result, Does.Contain("| Spar / ignore this | - | - |"));
    }

    /// <summary>The supermarkets near the user, framed as map data, with the province to price in.</summary>
    [Test]
    public async Task ShopsNear_AreAFramedTable_WithTheProvince()
    {
        const string near =
            """[{"name":"Malmesbury Superspar","lat":"-33.462","lon":"18.73","category":"shop","type":"supermarket","address":{"road":"Rood Street","town":"Malmesbury"}}]""";

        string result = await Tools(near).FindShopsNear("Malmesbury");

        Assert.That(result, Does.StartWith("--- untrusted map data from OpenStreetMap"));
        Assert.That(result, Does.Contain("Use Western Cape for estimated prices."));
        Assert.That(result, Does.Contain("Supermarkets near Malmesbury on OpenStreetMap (1), the nearest first:"));
        Assert.That(result, Does.Contain("| Malmesbury Superspar | Rood Street | Malmesbury |"));
        Assert.That(result, Does.EndWith("--- end of map data ---"));
    }

    /// <summary>None nearby, no town, or no map each say what to do.</summary>
    [Test]
    public async Task NoShops_NoTown_OrNoArgument_SayWhatToDo()
    {
        Assert.That(await Tools("[]").FindShopsNear("Malmesbury"),
            Does.Contain("OpenStreetMap lists no supermarket within about 55 km of Malmesbury."));
        Assert.That(await new WhereToBuyTools(new BranchFinder(Answering(_ => "[]").Map)).FindShopsNear("Nowhere"),
            Does.Contain("doesn't know the town 'Nowhere'"));
        Assert.That(await Tools("[]").FindShopsNear(" "), Does.Contain("FindShopsNear needs the user's town"));
    }
}
