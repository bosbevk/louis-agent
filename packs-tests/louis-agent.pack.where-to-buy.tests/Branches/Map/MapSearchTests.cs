using System.Net;
using LouisAgent.Packs.WhereToBuy.Branches;
using LouisAgent.Packs.WhereToBuy.Branches.Map;

namespace LouisAgent.Packs.WhereToBuy.Tests.Branches.Map;

/// <summary>
/// The map lookups: the URLs send only the town and the shop's name, in South Africa; Nominatim's answer is read into
/// places with their addresses; and the map not answering is said as such, never as "no town".
/// </summary>
public class MapSearchTests
{
    /// <summary>A fixture in the shape of Nominatim's jsonv2 answer (coordinates approximate, for these tests).</summary>
    internal static string Fixture(string name) => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "packs-tests/louis-agent.pack.where-to-buy.tests", "Branches", "Fixtures", name));

    /// <summary>A map that answers every request with this body, and counts the requests.</summary>
    internal static (MapSearch Map, List<Uri> Asked) Answering(Func<Uri, string> body, HttpStatusCode status = HttpStatusCode.OK)
    {
        var asked = new List<Uri>();
        var http = new HttpClient(new StubHandler(request =>
        {
            asked.Add(request.RequestUri!);
            return new HttpResponseMessage(status) { Content = new StringContent(body(request.RequestUri!)) };
        }));
        return (new MapSearch(http, TimeSpan.Zero), asked);
    }

    /// <summary>The town is its best match on the map in South Africa, with its address.</summary>
    [Test]
    public void TheTownLookup_IsTheTownInSouthAfrica_WithItsAddress() =>
        Assert.That(MapSearch.TownUrl(" Malmesbury, Wes-Kaap ").AbsoluteUri, Is.EqualTo(
            "https://nominatim.openstreetmap.org/search?format=jsonv2&addressdetails=1&countrycodes=za&limit=1&q=Malmesbury%2C%20Wes-Kaap"));

    /// <summary>The shop is searched by name inside a box around the town, and only there.</summary>
    [Test]
    public void TheShopLookup_IsBoundedToABoxAroundTheTown() =>
        Assert.That(MapSearch.NearUrl("Pick n Pay", -33.5, 18.75, 0.5).AbsoluteUri, Is.EqualTo(
            "https://nominatim.openstreetmap.org/search?format=jsonv2&addressdetails=1&countrycodes=za&limit=20&bounded=1&viewbox=18.25,-33,19.25,-34&q=Pick%20n%20Pay"));

    /// <summary>Nominatim's answer is read into the town, its coordinates from text, its municipality and province.</summary>
    [Test]
    public async Task ATownsAnswer_IsReadWithItsAddress()
    {
        var (map, _) = Answering(_ => Fixture("nominatim-town-malmesbury.json"));

        var town = await map.TownAsync("Malmesbury", CancellationToken.None);

        Assert.That((town!.Name, town.Latitude, town.Longitude), Is.EqualTo(("Malmesbury", -33.460278, 18.723611)));
        Assert.That((town.Address!.Area, town.Address.State), Is.EqualTo(("Swartland Local Municipality", "Western Cape")));
    }

    /// <summary>A town the map doesn't know is no town.</summary>
    [Test]
    public async Task ATownTheMapDoesntKnow_IsNone() =>
        Assert.That(await Answering(_ => "[]").Map.TownAsync("Nowhere", CancellationToken.None), Is.Null);

    /// <summary>A 429, a 503 or a page that isn't JSON is the map not answering, never "no town".</summary>
    [TestCase(HttpStatusCode.TooManyRequests, "[]")]
    [TestCase(HttpStatusCode.ServiceUnavailable, "[]")]
    [TestCase(HttpStatusCode.OK, "<html>blocked</html>")]
    public void TheMapNotAnswering_IsUnavailable(HttpStatusCode status, string body) =>
        Assert.That(() => Answering(_ => body, status).Map.TownAsync("Malmesbury", CancellationToken.None),
            Throws.TypeOf<MapUnavailableException>());

    /// <summary>The log says the map failed; the town the user named is a keyed hash and a length.</summary>
    [Test]
    [NonParallelizable]
    public void TheMapNotAnswering_LogsTheTownAsAHash()
    {
        using var stderr = new CapturedStderr();

        Assert.That(() => Answering(_ => "<html>", HttpStatusCode.OK).Map.TownAsync("Malmesbury", CancellationToken.None),
            Throws.TypeOf<MapUnavailableException>());

        Assert.That(stderr.Text, Does.Contain("OpenStreetMap didn't answer a lookup for #").And.Contain("(10)").And.Not
            .Contain("Malmesbury"));
    }
}
