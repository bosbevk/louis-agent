using LouisAgent.Packs.Shops;

namespace LouisAgent.Packs.Shops.Tests;

// F34 step 1b: a shop that serves by area needs the user's town as coordinates. OpenStreetMap's Nominatim gives a town's
// centre (the cook already asks it for the town's shops); only the town is sent, never the user's address. The fixture
// is its real answer for Malmesbury, recorded 2026-10-17 and trimmed.
public class PlaceCoordinatesTests
{
    private static string Fixture => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.pack.shops.tests", "Fixtures", "nominatim-malmesbury.json"));

    [Test]
    public void TheLookup_IsTheTownOnOpenStreetMap_InSouthAfrica() =>
        Assert.That(PlaceCoordinates.Url(" Malmesbury, Wes-Kaap ").AbsoluteUri,
            Is.EqualTo("https://nominatim.openstreetmap.org/search?q=Malmesbury%2C%20Wes-Kaap&format=json&limit=1&countrycodes=za"));

    [Test]
    public void ATownsAnswer_IsItsCentre() =>
        Assert.That(PlaceCoordinates.Parse(Fixture), Is.EqualTo(((double, double)?)(-33.460278, 18.723611)));

    [TestCase("[]")]
    [TestCase("[{\"name\":\"no coordinates\"}]")]
    public void NoTown_IsNoCentre(string answer) =>
        Assert.That(PlaceCoordinates.Parse(answer), Is.Null);

    // Review of PR #61: a 429, a 503 or a timeout also came back as "no town", and the store cache kept that a day. A town
    // Nominatim doesn't know is an answer; Nominatim not answering is no answer, and says so.
    [TestCase(System.Net.HttpStatusCode.TooManyRequests, "[]")]
    [TestCase(System.Net.HttpStatusCode.ServiceUnavailable, "[]")]
    [TestCase(System.Net.HttpStatusCode.OK, "<html>blocked</html>")]
    public void NominatimNotAnswering_IsNoAnswer_NotNoTown(System.Net.HttpStatusCode status, string body)
    {
        var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(status) { Content = new StringContent(body) }));

        Assert.That(() => PlaceCoordinates.OfAsync("Malmesbury", http, CancellationToken.None), Throws.TypeOf<PlaceUnavailableException>());
    }

    [Test]
    public async Task ATownNominatimDoesntKnow_IsNoCentre()
    {
        var http = new HttpClient(new StubHandler(_ => new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("[]") }));

        Assert.That(await PlaceCoordinates.OfAsync("Atlantis", http, CancellationToken.None), Is.Null);
    }
}
