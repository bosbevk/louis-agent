using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Content.Build;
using LouisAgent.Web.Framework.Pages.Build;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages.Build;

/// <summary>
/// Build your own, at <c>/build-your-own</c>: an identity, a pack and a site of your own, each told in a few steps
/// and handed on to the guide that walks it through. The folders its steps name are folders of the repo, so the page
/// can't send a reader to a place that isn't there.
/// </summary>
public class BuildYourOwnPageTests
{
    /// <summary>A context the framework's pages draw in: the docs' address their links read.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));
        // The site's words: the code listing's copy button reads them.
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>It answers at its address with its heading, then a section for each thing you build, in the menu.</summary>
    [Test]
    public void ThePage_HasItsHeading_AndASectionForEachThing()
    {
        using var ctx = Context();

        var cut = ctx.Render<BuildYourOwnPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent.Trim(), Is.EqualTo("Make an agent of your own."));
            Assert.That(cut.FindAll("section[id]").Select(section => section.Id),
                Is.EqualTo(new[] { "top", "an-identity", "a-pack", "a-site" }));
            Assert.That(FrameworkContents.All.Pages.Select(entry => entry.Address), Does.Contain("/build-your-own"),
                "in the menu");
        });
    }

    /// <summary>Each section hands on to the guide that walks it through.</summary>
    [Test]
    public void EachSection_LinksItsGuide()
    {
        using var ctx = Context();

        var cut = ctx.Render<BuildYourOwnPage>();

        Assert.Multiple(() =>
        {
            Assert.That(Guide(cut, "an-identity"), Has.Some.EndsWith("guides/build-an-identity.md"));
            Assert.That(Guide(cut, "a-pack"), Has.Some.EndsWith("guides/build-a-pack.md"));
            Assert.That(Guide(cut, "a-site"), Has.Some.EndsWith("guides/spawn-a-site.md"));
        });
    }

    /// <summary>Every folder a step names, in code font, is a folder of the repo.</summary>
    [Test]
    public void EveryFolderTheStepsName_IsInTheRepo()
    {
        var named = BuildYourOwn.Folders;

        Assert.Multiple(() =>
        {
            Assert.That(named, Is.Not.Empty);
            foreach (string folder in named)
                Assert.That(Directory.Exists(Path.Combine(TestPaths.RepoRoot, folder)), folder);
        });
    }

    /// <summary>The addresses a section's doc links lead to.</summary>
    private static IEnumerable<string?> Guide(IRenderedComponent<BuildYourOwnPage> cut, string section) =>
        cut.FindAll($"section#{section} a.doc-link").Select(link => link.GetAttribute("href"));
}
