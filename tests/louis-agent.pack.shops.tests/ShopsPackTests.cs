using LouisAgent.Packs.Shops;
using LouisAgent.Choices;
using LouisAgent.Core.Skills;

namespace LouisAgent.Packs.Shops.Tests;

// CL15 step 4: the shops are a pack (ADR 0027). Its guide is its own, read through the one prompt reader once a host
// registers the pack's prompts (ShopsPrompts does here, as AgentHost.Build does with the catalogue's), and core's
// prompts hold no shops guide any more.
public class ShopsPackTests
{
    [Test]
    public void TheGuide_ComesFromThePack_NotFromCore()
    {
        var pack = new ShopsPack(new ShopsOptions());

        Assert.That(pack.Name, Is.EqualTo(ShopsToolset.Shops.Wire()));
        Assert.That(pack.Toolsets.Select(t => t.Name), Is.EqualTo(new[] { ShopsToolset.Shops.Wire() }));
        Assert.That(pack.Toolsets[0].GuideFile, Is.EqualTo("toolsets/shops.md"));
        Assert.That(pack.Prompts.GetFileInfo("toolsets/shops.md").Exists, Is.True, "the project's manifest lines embed it");
        Assert.That(SkillFileSource.CoreFiles, Has.None.EqualTo("toolsets/shops.md"), "and core's don't");
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/shops.md"), Does.Contain("Look for deals first"), "read through the one reader");
    }

    [Test]
    public void TheGuide_RoutesAShopQuestionHere_AndHoldsNoProcedure()
    {
        // The routing row left the coding agent's mode for the guide (a pack routes what it does); a pack's guide is
        // prose, convertible to an MCP server's instructions (ADR 0028).
        string guide = SkillFileSource.ReadEmbedded("toolsets/shops.md")!;

        Assert.That(guide, Does.Contain("Route here a request about a shop's products and prices"));
        Assert.That(guide, Does.Not.Contain("## Skill:"));
    }
}
