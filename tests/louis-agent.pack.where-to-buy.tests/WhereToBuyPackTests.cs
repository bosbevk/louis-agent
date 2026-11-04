using LouisAgent.Choices;
using LouisAgent.Core.Skills;
using LouisAgent.Packs.WhereToBuy.Tools;

namespace LouisAgent.Packs.WhereToBuy.Tests;

/// <summary>The pack is one toolset with its guide, and needs nothing set to load.</summary>
public class WhereToBuyPackTests
{
    /// <summary>Its name, toolset and guide are the pack's own, read through the one reader, not core's.</summary>
    [Test]
    public void TheGuide_ComesFromThePack_NotFromCore()
    {
        var pack = new WhereToBuyPack();

        Assert.That(pack.Name, Is.EqualTo(WhereToBuyToolset.WhereToBuy.Wire()));
        Assert.That(pack.Toolsets.Select(t => t.Name), Is.EqualTo(new[] { WhereToBuyToolset.WhereToBuy.Wire() }));
        Assert.That(pack.Toolsets[0].GuideFile, Is.EqualTo("toolsets/where-to-buy.md"));
        Assert.That(SkillFileSource.CoreFiles, Has.None.EqualTo("toolsets/where-to-buy.md"), "core's don't hold it");
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/where-to-buy.md"),
            Does.Contain("Route here a question about where to buy something").And.Not.Contain("## Skill:"));
    }

    /// <summary>No setting and no browser: it loads wherever it is named, and its lookups run side by side.</summary>
    [Test]
    public void ItNeedsNoSetting_AndRunsInParallel()
    {
        var toolset = new WhereToBuyPack().Toolsets[0];

        Assert.That(toolset.MissingSettings(null!), Is.Empty);
        Assert.That(toolset.ParallelSafe, Is.True);
        Assert.That(toolset.ToolTypes, Is.EqualTo(new[] { typeof(WhereToBuyTools), typeof(ShopTools) }),
            "FindBranches stays, and FindShops sits beside it");
    }
}
