using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Content.Examples;
using LouisAgent.Web.Framework.Pages.Decide;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages.Decide;

/// <summary>
/// F162.1, step 12: Examples, at <c>/examples</c>, shows the agents the framework already runs, each named by the
/// identity that makes it, which the test finds on the disk, and what's coming as its last section.
/// </summary>
public class ExamplesPageTests
{
    /// <summary>A context the framework's pages draw in: the docs' address their links read.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(DocsAddress.GitHub("bosbevk/louis-agent", "main"));
        return ctx;
    }

    /// <summary>It answers with its heading, an example a section, what's coming last, and is in the menu.</summary>
    [Test]
    public void ThePage_HasItsHeading_AnExampleASection_AndWhatsComingLast()
    {
        using var ctx = Context();

        var cut = ctx.Render<ExamplesPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent.Trim(), Is.EqualTo("Agents it already runs."));
            Assert.That(cut.FindAll("section[id]").Select(section => section.Id),
                Is.EqualTo(new[] { "top", "puls-assistant", "the-coding-agent", "the-guide", "whats-coming" }));
            Assert.That(FrameworkContents.All.Pages.Select(entry => entry.Address), Does.Contain("/examples"));
        });
    }

    /// <summary>
    /// The page opens on its agents at a glance (Louis's pick A of three): the one engine, then a card each, named by
    /// its identity, read from the examples' own list.
    /// </summary>
    [Test]
    public void ThePage_OpensOnItsAgentsAtAGlance()
    {
        using var ctx = Context();

        string picture = ctx.Render<ExamplesPage>().Find("section#top figure.diagram-figure svg").InnerHtml;

        Assert.Multiple(() =>
        {
            Assert.That(picture, Does.Contain("louis-agent.core"), "the one engine");
            foreach (var example in FrameworkExamples.All)
            {
                Assert.That(picture, Does.Contain(example.Name));
                Assert.That(picture, Does.Contain(Path.GetFileName(example.Identity)));
            }
        });
    }

    /// <summary>Every example names the identity that makes it, a folder on the disk, and the page shows it.</summary>
    [Test]
    public void EveryExample_NamesItsIdentity_OnTheDisk()
    {
        using var ctx = Context();

        string page = ctx.Render<ExamplesPage>().Markup;

        Assert.Multiple(() =>
        {
            foreach (var example in FrameworkExamples.All)
            {
                Assert.That(Directory.Exists(Path.Combine(TestPaths.RepoRoot, example.Identity)), example.Identity);
                Assert.That(page, Does.Contain(Path.GetFileName(example.Identity)), $"{example.Identity} on the page");
            }
        });
    }
}
