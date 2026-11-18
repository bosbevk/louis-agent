using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Content.Features;
using LouisAgent.Web.Framework.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages;

/// <summary>
/// F164.1, step 11: Features, at <c>/features</c>, says what the framework does, by area. Each feature names the code
/// that does it and the doc that tells it, and both are held to the repo, so a feature that is taken out fails the
/// page.
/// </summary>
public class FeaturesPageTests
{
    /// <summary>A context the framework's pages draw in: the docs' address their links read.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));
        return ctx;
    }

    /// <summary>The framework's own source, its code and its components, but not its tests.</summary>
    private static string Source() => string.Join("\n",
        new[] { "*.cs", "*.razor" }.SelectMany(pattern => Directory.EnumerateFiles(
                Path.Combine(TestPaths.RepoRoot, "src"), pattern, SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}tests{Path.DirectorySeparatorChar}")
                           && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText));

    /// <summary>It answers with its heading, then an area a section, each an address of its own.</summary>
    [Test]
    public void ThePage_HasItsHeading_AndAnAreaASection()
    {
        using var ctx = Context();

        var cut = ctx.Render<FeaturesPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent.Trim(), Is.EqualTo("Everything it does."));
            Assert.That(cut.FindAll("section[id]").Select(section => section.Id), Is.EqualTo(new[]
            {
                "top", "the-engine", "the-models", "the-hosts", "the-web", "the-guards", "the-cost", "the-code"
            }));
            // Every feature, not a few (Louis: "research all our features and list them this is why people would
            // want it").
            Assert.That(FrameworkFeatures.All, Has.Count.GreaterThanOrEqualTo(30));
            Assert.That(FrameworkContents.All.Pages.Select(entry => entry.Address), Does.Contain("/features"),
                "in the menu");
        });
    }

    /// <summary>
    /// The page opens on its areas at a glance (Louis's pick A of three): a card an area, each with its count of
    /// features, read from the list so the picture can't drift from it.
    /// </summary>
    [Test]
    public void ThePage_OpensOnItsAreasAtAGlance()
    {
        using var ctx = Context();

        string picture = ctx.Render<FeaturesPage>().Find("section#top figure.diagram-figure svg").InnerHtml;

        Assert.Multiple(() =>
        {
            foreach (var area in FrameworkFeatures.Areas)
            {
                int count = FrameworkFeatures.All.Count(feature => feature.Area == area);
                Assert.That(picture, Does.Contain(area.Name), "the area");
                Assert.That(picture, Does.Contain($"{count} {area.Counts}"), $"{area.Name}'s count");
            }
        });
    }

    /// <summary>
    /// Every feature's code is in the framework's source and its doc is a file, and the page shows each code, the
    /// ones the plan names among them.
    /// </summary>
    [Test]
    public void EveryFeature_NamesRealCode_AndARealDoc()
    {
        using var ctx = Context();
        string source = Source();

        string page = ctx.Render<FeaturesPage>().Markup;

        Assert.Multiple(() =>
        {
            foreach (var feature in FrameworkFeatures.All)
            {
                Assert.That(source, Does.Contain(feature.Code), $"{feature.Name}'s code");
                Assert.That(File.Exists(Path.Combine(TestPaths.RepoRoot, feature.Doc)), $"{feature.Name}'s doc");
                Assert.That(page, Does.Contain(feature.Code), $"{feature.Name} on the page");
            }

            Assert.That(FrameworkFeatures.All.Select(feature => feature.Code),
                Is.SupersetOf(new[] { "STREAM_PACE", "Stream-Paced", "SessionReset", "LookSwitch" }));
        });
    }
}
