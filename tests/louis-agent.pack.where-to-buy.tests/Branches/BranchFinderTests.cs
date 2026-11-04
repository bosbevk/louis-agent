using LouisAgent.Packs.WhereToBuy.Branches;
using static LouisAgent.Packs.WhereToBuy.Tests.Branches.Map.MapSearchTests;
using LouisAgent.Packs.WhereToBuy.Branches.Map;

namespace LouisAgent.Packs.WhereToBuy.Tests.Branches;

/// <summary>
/// A shop's branches near the town: shops only, by any of the shop's names, the nearest three first, each marked when
/// it is in the town's municipality; a wider box only when the first holds none.
/// </summary>
public class BranchFinderTests
{
    /// <summary>The town answers its lookup; every shop search answers with this.</summary>
    private static (MapSearch Map, List<Uri> Asked) Map(string near) =>
        Answering(url => url.Query.Contains("limit=1&")
            ? Fixture("nominatim-town-malmesbury.json")
            : near);

    /// <summary>The nearest three shops of the name, the nearest first, a street that shares the name left out.</summary>
    [Test]
    public async Task TheNearestThreeBranches_AreKept_NearestFirst()
    {
        var (map, asked) = Map(Fixture("nominatim-pnp-near-malmesbury.json"));

        var search = await new BranchFinder(map).FindAsync("PnP", "Malmesbury", CancellationToken.None);

        Assert.That(search!.Shop, Is.EqualTo("Pick n Pay"), "PnP is searched as the name the map uses");
        Assert.That(search.Branches.Select(b => b.Name), Is.EqualTo(new[]
        {
            "Pick n Pay Malmesbury", "Pick n Pay Express", "PnP Family Riebeek Kasteel"
        }));
        Assert.That(asked, Has.Count.EqualTo(2), "the town, then one box was enough");
    }

    /// <summary>A branch carries its street, its place, its distance and whether it is in the town's municipality.</summary>
    [Test]
    public async Task ABranch_HasItsStreetPlaceDistance_AndArea()
    {
        var (map, _) = Map(Fixture("nominatim-pnp-near-malmesbury.json"));

        var search = await new BranchFinder(map).FindAsync("Pick n Pay", "Malmesbury", CancellationToken.None);

        var town = search!.Branches[0];
        Assert.That((town.Street, town.Place, town.InTownsArea), Is.EqualTo(("12 Voortrekker Road", "Malmesbury", true)));
        Assert.That(town.Km, Is.EqualTo(0.5).Within(0.1));
        Assert.That(search.Branches[2].Km, Is.EqualTo(18).Within(1));
    }

    /// <summary>With none in the first box, a wider one is searched; with none there either, the search says so.</summary>
    [Test]
    public async Task NoBranchInEitherBox_IsNone_AfterTheWiderBox()
    {
        var (map, asked) = Map("[]");

        var search = await new BranchFinder(map).FindAsync("Checkers", "Malmesbury", CancellationToken.None);

        Assert.That(search!.Branches, Is.Empty);
        Assert.That(search.SearchedKm, Is.EqualTo(166));
        Assert.That(asked, Has.Count.EqualTo(3));
        Assert.That(asked[2].Query, Does.Contain("viewbox=17.22"));
    }

    /// <summary>A town the map doesn't know is no search at all.</summary>
    [Test]
    public async Task ATownTheMapDoesntKnow_IsNoSearch() =>
        Assert.That(await new BranchFinder(Answering(_ => "[]").Map).FindAsync("Spar", "Nowhere", CancellationToken.None),
            Is.Null);
}
