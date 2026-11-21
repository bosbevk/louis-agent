using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Site.Showroom;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Web.Tests.Components.Site.Showroom;

/// <summary>
/// F127.1: a site's demos are data it registers once, and the showroom draws its rail from them: grouped by what the
/// visitor wants (the research's first rule), each group in its order, and the demo at an address found for the rail
/// to light.
/// </summary>
public class ShowroomDemosTests
{
    /// <summary>Two demos for a business, one at work, given out of group order on purpose.</summary>
    private static readonly ShowroomDemos Demos = new([
        new ShowroomDemo("/bakery-demo", "A bakery", "Bread at dawn", DemoGroup.ForYourBusiness, TileAccent.Green,
            DiagramIcon.Bed),
        new ShowroomDemo("/coder-demo", "A coder", "Fixes a bug", DemoGroup.AgentsAtWork, TileAccent.Red,
            DiagramIcon.Code),
        new ShowroomDemo("/florist-demo", "A florist", "Flowers by noon", DemoGroup.ForYourBusiness, TileAccent.Blue,
            DiagramIcon.Dining),
    ]);

    /// <summary>The groups come in their own order, and the demos in each in the order the site gave them.</summary>
    [Test]
    public void TheGroups_ComeInOrder_EachWithItsDemosInOrder()
    {
        var groups = Demos.Groups.Select(g => (g.Key, Names: string.Join(", ", g.Select(d => d.Name)))).ToList();

        Assert.That(groups,
            Is.EqualTo(new[]
            {
                (DemoGroup.ForYourBusiness, "A bakery, A florist"), (DemoGroup.AgentsAtWork, "A coder"),
            }));
    }

    /// <summary>
    /// The demo at an address is found whatever its case or a trailing slash; another address has none.
    /// </summary>
    [TestCase("/florist-demo", "A florist")]
    [TestCase("/Florist-Demo/", "A florist")]
    [TestCase("/pricing", null)]
    [TestCase("/", null)]
    public void TheDemoAtAnAddress_IsFound(string path, string? name) =>
        Assert.That(Demos.At(path)?.Name, Is.EqualTo(name));
}
