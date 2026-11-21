using LouisAgent.Site.Content.Trust;
using LouisAgent.Site.Pages.About;
using LouisAgent.Web.Components.Site.Tiles;

namespace LouisAgent.Site.Tests.Pages.About;

/// <summary>
/// The trust page (F165): real technical facts for a business owner deciding whether to trust the site, each one
/// saying whether it is in place or not yet (Louis's pick, "Status on every tile"), each in place one held to a file
/// of the repo, and the reviewer's checklist answered where it stands.
/// </summary>
public class TrustPageTests
{
    /// <summary>The page's five parts, each at its own address in words.</summary>
    [Test]
    public void ThePage_HasItsParts_EachAtItsAddress()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<TrustPage>();

        var ids = cut.FindAll("section").Select(section => section.GetAttribute("id")).ToList();
        Assert.That(ids, Is.EqualTo(new[]
        {
            "top", "where-it-runs", "your-data", "ai-safety", "if-something-breaks", "before-paying-customers"
        }));
    }

    /// <summary>Every fact on the page is a tile that says In place or Not yet, and both kinds are there.</summary>
    [Test]
    public void EveryFact_SaysWhetherItIsInPlace()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<TrustPage>();

        var tiles = cut.FindAll("section:not(#top) article.panel");
        var words = tiles.Select(tile => tile.QuerySelector(".tile-status")?.TextContent).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(tiles, Has.Count.EqualTo(TrustFacts.All.Count));
            Assert.That(words, Is.All.AnyOf("In place", "Not yet"));
            Assert.That(words, Does.Contain("In place").And.Contain("Not yet"));
        });
    }

    /// <summary>Every fact in place names the file of the repo that shows it, and the file is there.</summary>
    [Test]
    public void EveryFactInPlace_IsHeldToAFileOfTheRepo()
    {
        var done = TrustFacts.All.Where(fact => fact.Status == TileStatus.Done).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(done, Is.Not.Empty);
            foreach (var fact in done)
                Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, fact.Source ?? "")),
                    $"{fact.Title}: {fact.Source}");
        });
    }

    /// <summary>
    /// No fact rests on a demo's file: a demo can leave and take its files with it, and the fact would point at nothing
    /// (FX215, Puls's Bicep file).
    /// </summary>
    [Test]
    public void NoFact_RestsOnADemosFile() =>
        Assert.That(TrustFacts.All.Where(fact => fact.Source?.StartsWith("demos/", StringComparison.Ordinal) == true)
            .Select(fact => $"{fact.Title}: {fact.Source}"), Is.Empty);

    /// <summary>
    /// The companies that handle data for the site are named in a table, the model's maker among them.
    /// </summary>
    [Test]
    public void TheCompaniesThatHandleData_AreNamed()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<TrustPage>();

        var names = cut.FindAll("section#your-data table.comparison tbody th").Select(cell => cell.TextContent);
        Assert.That(names, Is.EqualTo(new[] { "Microsoft Azure", "Anthropic", "Resend", "Cloudflare", "GitHub" }));
    }

    /// <summary>The words a visitor reads hold no semicolon and no colon (the site's rule for its pages).</summary>
    [Test]
    public void TheWords_HoldNoSemicolonOrColon()
    {
        using var ctx = SitePageContext.Create();

        var cut = ctx.Render<TrustPage>();

        string words = string.Join(" ", cut.FindAll("section").Select(section => section.TextContent));
        Assert.That(words, Does.Not.Contain(";").And.Not.Contain(":"));
    }
}
