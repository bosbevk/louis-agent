using LouisAgent.Choices;
using LouisAgent.Packs.Site.Tools;

namespace LouisAgent.Packs.Site.Tests;

/// <summary>The pack is one toolset with its guide (F117): always on, one tool, the guide says to say one line about the page.</summary>
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

    /// <summary>It needs no setting and builds its one tool.</summary>
    [Test]
    public void ItIsAlwaysOn()
    {
        var toolset = new SitePack().Toolsets[0];

        Assert.Multiple(() =>
        {
            Assert.That(toolset.MissingSettings(null!), Is.Empty);
            Assert.That(toolset.ToolTypes, Is.EqualTo(new[] { typeof(SiteTools) }));
            Assert.That(toolset.CreateTools(null!).Single(), Is.TypeOf<SiteTools>());
        });
    }
}
