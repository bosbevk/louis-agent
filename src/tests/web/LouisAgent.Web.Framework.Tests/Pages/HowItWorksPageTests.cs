using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages;

/// <summary>
/// F164.1, step 10: How it works, at <c>/how-it-works</c>, follows one turn through every layer, then the web with the
/// backend, every identity, every pack, and the rules every turn keeps. Its identities and packs are read from the
/// disk, so a new one can't be left off the page.
/// </summary>
public class HowItWorksPageTests
{
    /// <summary>A context the framework's pages draw in: the docs' address their links read.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));
        return ctx;
    }

    /// <summary>The folders directly under a folder of the repo, by name.</summary>
    private static IEnumerable<string> Folders(string under) =>
        Directory.GetDirectories(Path.Combine(TestPaths.RepoRoot, under)).Select(Path.GetFileName).OfType<string>();

    /// <summary>
    /// Every identity in the repo, a project named <c>LouisAgent.Identities.*</c> in a demo's <c>identity/</c> or the
    /// site's folder; its evals and judges are not identities.
    /// </summary>
    private static List<string> Identities() =>
    [
        .. new[] { "demos/louis-agent/identity", "src/site" }.SelectMany(Folders)
            .Where(name => name.StartsWith(ProjectNames.IdentityPrefix, StringComparison.Ordinal))
    ];

    /// <summary>It answers with its heading, then its five sections, each an address of its own, and the turn drawn.</summary>
    [Test]
    public void ThePage_HasItsHeading_ItsSections_AndTheTurnDrawn()
    {
        using var ctx = Context();

        var cut = ctx.Render<HowItWorksPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent.Trim(), Is.EqualTo("One turn, through every layer."));
            Assert.That(cut.FindAll("section[id]").Select(section => section.Id),
                Is.EqualTo(new[] { "top", "the-web", "identities", "packs", "the-rules" }));
            Assert.That(cut.Find("section#top figure.diagram-figure svg").InnerHtml,
                Does.Contain("StreamPromptAsync"), "the turn, by the engine's own call");
            Assert.That(FrameworkContents.All.Pages.Select(entry => entry.Address), Does.Contain("/how-it-works"),
                "in the menu");
        });
    }

    /// <summary>Every identity in the repo is named on the page.</summary>
    [Test]
    public void EveryIdentity_IsNamed()
    {
        using var ctx = Context();
        var identities = Identities();

        string page = ctx.Render<HowItWorksPage>().Find("section#identities").TextContent;

        Assert.Multiple(() =>
        {
            Assert.That(identities, Has.Count.GreaterThanOrEqualTo(5));
            Assert.That(identities.Where(name => !page.Contains(name)), Is.Empty);
        });
    }

    /// <summary>Every pack in <c>packs/tools/</c> is named on the page.</summary>
    [Test]
    public void EveryPack_IsNamed()
    {
        using var ctx = Context();
        var packs = Folders("packs/tools").ToList();

        string page = ctx.Render<HowItWorksPage>().Find("section#packs").TextContent;

        Assert.Multiple(() =>
        {
            Assert.That(packs, Has.Count.GreaterThanOrEqualTo(12));
            Assert.That(packs.Where(name => !page.Contains(name)), Is.Empty);
        });
    }
}
