using LouisAgent.Choices;
using LouisAgent.Packs.Site.Messages;
using LouisAgent.Packs.Site.Tools;
using LouisAgent.Testing.Messages;

namespace LouisAgent.Packs.Site.Tests;

/// <summary>
/// The pack is one toolset with its guide (F117): always on, its tools the pages' and (F127.4.1) the cards', and the
/// guide says to say one line about the page.
/// </summary>
public class SitePackTests
{
    /// <summary>Its name, its toolset and its guide are the pack's own.</summary>
    [Test]
    public void TheGuide_ComesFromThePack()
    {
        var pack = new SitePack();

        using var reader = new StreamReader(pack.Prompts.GetFileInfo(pack.Toolsets[0].GuideFile!).CreateReadStream());
        string guide = reader.ReadToEnd();

        Assert.Multiple(() =>
        {
            Assert.That(pack.Name, Is.EqualTo(SiteToolset.Site.Wire()));
            Assert.That(pack.Toolsets.Select(t => t.Name), Is.EqualTo(new[] { SiteToolset.Site.Wire() }));
            Assert.That(pack.Toolsets[0].GuideFile, Is.EqualTo("toolsets/site.md"));
            Assert.That(guide, Does.Contain("ShowPage").And.Contain("one line"));
        });
    }

    /// <summary>
    /// F127.7, Louis: "each part on the page becomes a piece the agent can elaborate on, and focus on. like select
    /// it". The guide knows a piece by its marks, answers about the one the visitor selected, and shows a piece to
    /// select it for them.
    /// </summary>
    [Test]
    public void TheGuide_KnowsAPieceAndTheSelectedOne()
    {
        var pack = new SitePack();
        using var reader = new StreamReader(pack.Prompts.GetFileInfo(pack.Toolsets[0].GuideFile!).CreateReadStream());
        string guide = string.Join(' ', reader.ReadToEnd().Split((char[])[' ', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries));

        Assert.Multiple(() =>
        {
            Assert.That(guide, Does.Contain("`(piece, selects)`"));
            Assert.That(guide, Does.Contain("[The visitor selected the piece"));
            Assert.That(guide, Does.Contain("is pressed for them"));
            Assert.That(guide, Does.Contain("Whenever your words ask them something"),
                "FX147: a question in words comes with a choice that holds a box");
            Assert.That(guide, Does.Contain("a stop on a piece shows that piece by its own address, never the section"),
                "FX146: a walk over the tiles moves the selection with it");
            Assert.That(guide, Does.Contain("not on the page they are on, call `FindPlace` first, then `ShowPage`"),
                "Louis: \"from anywhre that matter\"; the site's own pages are the index, not a map in the prompt");
        });
    }

    /// <summary>Each message's holes record matches its template (ADR 0119).</summary>
    [Test]
    public void EveryHolesRecord_MatchesItsTemplate() =>
        Assert.That(MessageHoles.Problems([typeof(SiteToolMessage)]), Is.Empty);

    /// <summary>It needs no setting and builds its two tool classes, the pages' and (F127.4.1) the cards'.</summary>
    [Test]
    public void ItIsAlwaysOn()
    {
        var toolset = new SitePack().Toolsets[0];

        Assert.Multiple(() =>
        {
            Assert.That(toolset.MissingSettings(new NoPackContext()), Is.Empty);
            Assert.That(toolset.ToolTypes, Is.EqualTo(new[] { typeof(SiteTools), typeof(CardTools) }));
            Assert.That(toolset.CreateTools(new NoPackContext()).Select(tools => tools.GetType()),
                Is.EqualTo(new[] { typeof(SiteTools), typeof(CardTools) }));
        });
    }
}
