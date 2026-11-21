namespace LouisAgent.Web.Tests.Rules.Sites;

/// <summary>
/// F127.6: every site on the framework names its areas through the framework's builder, <c>AddSiteAreas</c>, and wires
/// none of an area's parts by hand: no frames, pack views or chat handoff of its own, no eval studio or accounts
/// registered beside the builder, and a router that reads the areas' pages instead of listing the libraries.
/// </summary>
/// <remarks>
/// What the builder replaced, held out by name, so a new site made by hand from an old one fails here and is pointed at
/// the builder rather than forgetting a part in silence.
/// </remarks>
[TestFixture]
public class SitesNameTheirAreasTests
{
    /// <summary>What a site no longer writes itself: each is an area's part now.</summary>
    private static readonly string[] HandWired =
    [
        "new SiteFrames(", "new PackViews(", "new ChatHandoff(", "AddLouisAgentEvals(", "AddLouisAgentAccounts(",
        "FrameworkPages.With("
    ];

    /// <summary>The sites that start themselves: a folder with a Program.cs.</summary>
    private static IEnumerable<string> Sites() =>
        FrameworkWebSites.Folders().Where(site => File.Exists(Path.Combine(site, "Program.cs")));

    /// <summary>Every site's program names its areas through the builder.</summary>
    [Test]
    public void EverySite_NamesItsAreas_ThroughTheBuilder()
    {
        var withoutBuilder = Sites()
            .Where(site => !File.ReadAllText(Path.Combine(site, "Program.cs")).Contains("AddSiteAreas("))
            .Select(FrameworkWebSites.NameOf).ToList();

        Assert.That(Sites().Count(), Is.GreaterThanOrEqualTo(4), "the sites were found");
        Assert.That(withoutBuilder, Is.Empty, "Name the site's areas with builder.Services.AddSiteAreas(...)");
    }

    /// <summary>No site wires an area's part by hand, in its program or its router.</summary>
    [Test]
    public void NoSite_WiresAnAreasPart_ByHand()
    {
        var byHand = Sites()
            .SelectMany(site => new[] { "Program.cs", "App.razor" }
                .Select(file => Path.Combine(site, file)).Where(File.Exists)
                .SelectMany(file => HandWired.Where(File.ReadAllText(file).Contains)
                    .Select(part => $"{FrameworkWebSites.NameOf(site)}/{Path.GetFileName(file)}: {part}")))
            .ToList();

        Assert.That(byHand, Is.Empty,
            "An area's part comes from its area in AddSiteAreas:\n  " + string.Join("\n  ", byHand));
    }
}
