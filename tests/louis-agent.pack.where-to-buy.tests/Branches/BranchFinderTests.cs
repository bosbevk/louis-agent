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

    /// <summary>As the real map answers: the smallest box with this, any wider box with the city's 20 hits.</summary>
    private static (MapSearch Map, List<Uri> Asked) MapBySize(string smallestBox) =>
        Answering(url => AnswerTo(url, smallestBox));

    /// <summary>One answer per kind of question, one guard clause each (ADR 0051).</summary>
    private static string AnswerTo(Uri url, string smallestBox)
    {
        if (url.Query.Contains("limit=1&")) return Fixture("nominatim-town-malmesbury.json");
        return url.Query.Contains("viewbox=18.5") ? smallestBox : TwentyInACity();
    }

    /// <summary>The nearest three shops of the name, nearest first, a street that shares the name left out.</summary>
    [Test]
    public async Task TheNearestThreeBranches_AreKept_NearestFirst()
    {
        var (map, asked) = Map(Fixture("nominatim-pnp-near-malmesbury.json"));

        var search = await new BranchFinder(map).FindAsync("PnP", "Malmesbury", CancellationToken.None);

        Assert.That(search!.Shop, Is.EqualTo("Pick n Pay"), "PnP is searched as the name the map uses");
        Assert.That(search.Branches.Select(b => b.Name),
            Is.EqualTo(new[] { "Pick n Pay Malmesbury", "Pick n Pay Express", "PnP Family Riebeek Kasteel" }));
        Assert.That(asked, Has.Count.EqualTo(2), "the town, then one box was enough");
    }

    /// <summary>A branch carries its street, place, distance and whether it is in the town's municipality.</summary>
    [Test]
    public async Task ABranch_HasItsStreetPlaceDistance_AndArea()
    {
        var (map, _) = Map(Fixture("nominatim-pnp-near-malmesbury.json"));

        var search = await new BranchFinder(map).FindAsync("Pick n Pay", "Malmesbury", CancellationToken.None);

        var town = search!.Branches[0];
        Assert.That((town.Street, town.Place, town.InTownsArea),
            Is.EqualTo(("12 Voortrekker Road", "Malmesbury", true)));
        Assert.That(town.Km, Is.EqualTo(0.5).Within(0.1));
        Assert.That(search.Branches[2].Km, Is.EqualTo(18).Within(1));
    }

    /// <summary>With none in any box, up to the widest, the search says so.</summary>
    [Test]
    public async Task NoBranchInEitherBox_IsNone_AfterTheWiderBox()
    {
        var (map, asked) = Map("[]");

        var search = await new BranchFinder(map).FindAsync("Checkers", "Malmesbury", CancellationToken.None);

        Assert.That(search!.Branches, Is.Empty);
        Assert.That(search.SearchedKm, Is.EqualTo(166));
        Assert.That(asked, Has.Count.EqualTo(4), "the town, then three boxes");
        Assert.That(asked[3].Query, Does.Contain("viewbox=17.22"));
    }

    /// <summary>
    /// The map gives 20 hits in no distance order, so a box that reaches a city fills with the city's branches. On
    /// 2026-11-04 it told a user Malmesbury has no Pick n Pay and named one 42.9 km away, when one is 0.5 km from the
    /// centre: the town's own branch has to be found before a box wide enough to lose it is asked.
    /// </summary>
    [Test]
    public async Task ATownsOwnBranch_IsFound_WhenACityFillsTheWiderBox()
    {
        var (map, asked) = MapBySize(Fixture("nominatim-pnp-near-malmesbury.json"));

        var search = await new BranchFinder(map).FindAsync("Pick n Pay", "Malmesbury", CancellationToken.None);

        Assert.That(search!.Branches[0].Name, Is.EqualTo("Pick n Pay Malmesbury"));
        Assert.That(search.Branches[0].Km, Is.EqualTo(0.5).Within(0.1));
        Assert.That(asked, Has.Count.EqualTo(2), "the town, then the smallest box was enough");
    }

    /// <summary>
    /// With none near the town the box still widens, so a town without the shop can still name its nearest: here
    /// the nearest are the city's, over 40 km away, and that answer has to survive the smaller first box.
    /// </summary>
    [Test]
    public async Task NoBranchNearby_WidensTheBox_AndNamesTheNearest()
    {
        var (map, asked) = MapBySize("[]");

        var search = await new BranchFinder(map).FindAsync("Pick n Pay", "Malmesbury", CancellationToken.None);

        Assert.That(search!.Branches, Has.Count.EqualTo(3));
        Assert.That(search.Branches[0].Km, Is.GreaterThan(40));
        Assert.That(search.SearchedKm, Is.EqualTo(55));
        Assert.That(asked, Has.Count.EqualTo(3), "the town, the small box, then the wider one");
    }

    /// <summary>What a wide box answers near Cape Town: the map's 20 hits, every one of them in the city.</summary>
    private static string TwentyInACity() =>
        "[" + string.Join(",", Enumerable.Range(1, 20).Select(number =>
            $$$"""
               {"lat":"-33.{{{850 + number}}}","lon":"18.5","category":"shop","type":"supermarket",
                "name":"Pick n Pay {{{number}}}","display_name":"Pick n Pay {{{number}}}, Cape Town, South Africa",
                "address":{"road":"Main Road","suburb":"Cape Town","city":"City of Cape Town","state":"Western Cape"}}
               """)) + "]";

    /// <summary>A town the map doesn't know is no search at all.</summary>
    [Test]
    public async Task ATownTheMapDoesntKnow_IsNoSearch() =>
        Assert.That(
            await new BranchFinder(Answering(_ => "[]").Map).FindAsync("Spar", "Nowhere", CancellationToken.None),
            Is.Null);
}
