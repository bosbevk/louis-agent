using LouisAgent.CodeReview.Content;
using LouisAgent.CodeReview.Pages.Site.Framework;

namespace LouisAgent.CodeReview.Tests.Content;

/// <summary>
/// The principles the site shows are the engine's (FX74, Louis: "this should be public"): the embedded copy is the
/// file in the engine, byte for byte, the front matter stays off the page, and the Identities page shows the four rules.
/// </summary>
public class PrinciplesTests
{
    /// <summary>The copy the site carries is the engine's file, exactly.</summary>
    [Test]
    public void TheSitesCopy_IsTheEnginesFile()
    {
        string engines = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/engine/louis-agent.core/Prompts/shared/principles.md"));

        Assert.That(Principles.Markdown.ReplaceLineEndings("\n"), Is.EqualTo(engines.ReplaceLineEndings("\n")));
    }

    /// <summary>The front matter is for the engine; the page gets what follows it.</summary>
    [Test]
    public void TheBody_LeavesTheFrontMatterOut()
    {
        string body = Principles.Body("---\nname: principles\n---\n# Principles\n\nThese hold.");

        Assert.That(body, Is.EqualTo("# Principles\n\nThese hold."));
    }

    /// <summary>The page shows the four rules, rendered, with no front matter in sight.</summary>
    [Test]
    public void ThePage_ShowsTheFourRules()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<IdentitiesPage>();
        string shown = cut.Find("#the-principles .principles").TextContent;

        Assert.Multiple(() =>
        {
            Assert.That(shown, Does.Contain("Never lie.").And.Contain("Never cheat.").And.Contain("Never steal.").And.Contain("Never murder."));
            Assert.That(shown, Does.Not.Contain("name: principles"));
        });
    }
}
