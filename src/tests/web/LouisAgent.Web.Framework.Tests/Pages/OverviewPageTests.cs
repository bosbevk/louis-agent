using LouisAgent.Choices;
using LouisAgent.Web.Docs;
using LouisAgent.Web.Framework.Content;
using LouisAgent.Web.Framework.Pages;
using LouisAgent.Web.Text;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Framework.Tests.Pages;

/// <summary>
/// F164.1, step 6: the Overview, at the framework site's root, opens with its heading and a picture at a glance, the
/// framework highlighted over its three important parts (Louis, of the hero: "do one like this highlight the
/// important ones"), then what the framework is with its real program, its whole map, and why C#.
/// </summary>
public class OverviewPageTests
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

    /// <summary>
    /// It opens with its heading and the picture at a glance, then its sections, each with an address, the program
    /// in what it is, and the whole framework drawn.
    /// </summary>
    [Test]
    public void TheOverview_OpensAtAGlance_ThenItsSections()
    {
        using var ctx = Context();

        var cut = ctx.Render<OverviewPage>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find("h1").TextContent.Trim(), Is.EqualTo("Build agents, not infrastructure."));
            Assert.That(cut.Find("section#top figure.diagram-figure svg").InnerHtml, Does.Contain("The engine"),
                "the picture at a glance, its important parts named");
            Assert.That(cut.Find("section#what-it-is figure.code-listing pre code").TextContent,
                Is.EqualTo(ProgramListings.RunTheCodingAgent));
            Assert.That(cut.FindAll("section[id]").Select(section => section.Id),
                Is.SupersetOf(new[] { "what-it-is", "the-whole", "why-csharp", "your-hardware" }));
            Assert.That(cut.FindAll("figure.diagram-figure svg"), Has.Count.EqualTo(6),
                "at a glance, then each of the five families under its own heading");
        });
    }

    /// <summary>
    /// The hero's program is the coding agent's real one, the lines that start it, so the page can't show a program
    /// that no longer runs.
    /// </summary>
    [Test]
    public void TheHerosProgram_IsTheCodingAgentsRealOne()
    {
        string program = File.ReadAllText(Path.Combine(TestPaths.RepoRoot,
            "demos/louis-agent/hosts/LouisAgent.CodingCli/Program.cs")).ReplaceLineEndings("\n");

        Assert.That(program, Does.Contain(ProgramListings.RunTheCodingAgent));
    }

    /// <summary>
    /// Each family stands under its own heading with its own overview picture (Louis: "instead of listing one big
    /// overview do each under a subheading and then use overview diagrams"), every project it ships named in it.
    /// </summary>
    [Test]
    public void EachFamily_HasItsHeadingAndItsPicture()
    {
        using var ctx = Context();

        var cut = ctx.Render<OverviewPage>();

        Assert.Multiple(() =>
        {
            foreach (var family in FrameworkFamily.All)
            {
                var section = cut.Find($"section#{family.Id}");
                Assert.That(section.QuerySelector("h2")!.TextContent.Trim(), Is.EqualTo(family.Name));
                string picture = section.QuerySelector("figure.diagram-figure svg")!.InnerHtml;
                Assert.That(family.Projects.Where(project => !picture.Contains(project.Name)), Is.Empty,
                    $"{family.Name} names every project it ships");
                // Its cards wear its colour (Louis: "give each one a color this color we can then reuse").
                Assert.That(picture, Does.Contain($"class=\"crd-tint tone-{family.Tone.Wire()}\""),
                    $"{family.Name}'s cards in its colour");
            }
        });
    }

    /// <summary>
    /// Beside the five families, the Overview names what is built on them, each by its folder, so a reader sees where
    /// a project of their own would sit; every folder it names is one in the repo.
    /// </summary>
    [Test]
    public void BuiltOnIt_NamesFoldersOfTheRepo()
    {
        using var ctx = Context();

        var cut = ctx.Render<OverviewPage>();

        Assert.Multiple(() =>
        {
            string section = cut.Find("section#built-on-it").TextContent;
            foreach (var folder in BuiltOnIt.Folders.SelectMany(term => term.Names))
            {
                Assert.That(Directory.Exists(Path.Combine(TestPaths.RepoRoot, folder)), $"{folder} is a folder");
                Assert.That(section, Does.Contain(folder), $"{folder} is on the page");
            }
        });
    }

    /// <summary>A section ends with the docs it was written from, each a file in the repo.</summary>
    [Test]
    public void TheSections_EndWithTheirDocs()
    {
        using var ctx = Context();

        var cut = ctx.Render<OverviewPage>();

        Assert.That(cut.FindAll("nav.doc-links a.doc-link"), Has.Count.GreaterThanOrEqualTo(4));
    }
}
