using LouisAgent.Core.Tools.Shops;

namespace LouisAgent.Core.Tests.Tools.Shops;

// F34 step 1b: a shop that serves by area needs the user's town as coordinates. OpenStreetMap's Nominatim gives a town's
// centre (the cook already asks it for the town's shops); only the town is sent, never the user's address. The fixture
// is its real answer for Malmesbury, recorded 2026-10-17 and trimmed.
public class PlaceCoordinatesTests
{
    private static string Fixture => File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "tests", "louis-agent.core.tests", "Tools", "Shops", "Fixtures", "nominatim-malmesbury.json"));

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
}
