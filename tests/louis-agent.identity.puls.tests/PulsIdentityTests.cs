using LouisAgent.Choices;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Packs.Recipes;
using LouisAgent.Packs.ShopPrices;
using LouisAgent.Packs.WhereToBuy;

namespace LouisAgent.Identities.Puls.Tests;

/// <summary>
/// Puls as the catalogue sees it: who it is, the packs it names, and the toolsets each of its roles declares in its
/// header.
/// </summary>
public class PulsIdentityTests
{
    /// <summary>
    /// Puls is named Puls, names the shops and recipes packs, and fits the catalogue once both are there.
    /// </summary>
    [Test]
    public void Puls_IsNamedPuls_AndFitsTheCatalogue()
    {
        var puls = new PulsIdentity();

        Assert.That(puls.Name, Is.EqualTo(PulsIdentityName.Puls.Wire()));
        Assert.That(puls.Packs,
            Is.EqualTo(new[] { PulsPack.WhereToBuy.Wire(), PulsPack.ShopPrices.Wire(), PulsPack.Recipes.Wire() }),
            "the cook's branches, prices (F50.2) and recipes are packs Puls names, in the identity's own enum " +
            "(ADR 0043), in this order");
        // Step 3: its prompts are its own, embedded with their paths (the manifest lines in its csproj work).
        Assert.That(puls.Prompts.GetFileInfo("agent/puls/voice.md").Exists, Is.True, "a mode");
        Assert.That(puls.Prompts.GetFileInfo("specialists/puls-cook.md").Exists, Is.True, "a specialist");
        Assert.That(puls.Prompts.GetFileInfo("minions/puls-minion.md").Exists, Is.True, "a minion base (F30, kept)");
        // The catalogue refuses an identity whose pack isn't there; with the packs, all are in.
        Assert.Throws<AgentCatalogException>(() => new AgentCatalog([puls], []), "packs named, none registered");
        var catalog = new AgentCatalog([puls],
            [new WhereToBuyPack(), new ShopPricesPack(), new RecipesPack(new RecipesOptions())]);
        Assert.That(catalog.Identities, Is.EqualTo(new[] { puls }));
        Assert.That(catalog.Toolsets.Select(t => t.Name),
            Does.Contain(WhereToBuyToolset.WhereToBuy.Wire()).And.Contain(ShopPricesToolset.ShopPrices.Wire()));
        Assert.That(catalog.Toolsets.Select(t => t.Name), Does.Contain(RecipesToolset.Recipes.Wire()));
    }

    /// <summary>
    /// The cook declares its recipes, where-to-buy and shop-prices toolsets in its header, required, and no other role
    /// declares one: a role carries what it always needs, so the cook can't run without the tools that read recipes,
    /// find the branch and estimate the prices.
    /// </summary>
    [Test]
    public void TheCookDeclaresItsToolsets_AndNoOtherRoleDoes()
    {
        // ADR 0025: the header's toolsets are read through the catalogue, with no `?`, so a missing pack refuses the
        // cook instead of quietly leaving its tools out.
        Assert.That(SkillLoader.ToolsetsOf(PulsRole.Cook.Wire(), RoleLevel.Specialist),
            Is.EqualTo(new[]
            {
                RecipesToolset.Recipes.Wire(), WhereToBuyToolset.WhereToBuy.Wire(), ShopPricesToolset.ShopPrices.Wire()
            }));

        foreach (string role in RoleCatalog.Available(RoleLevel.Specialist)
                     .Where(r => r != PulsRole.Cook.Wire()))
            Assert.That(SkillLoader.ToolsetsOf(role, RoleLevel.Specialist), Is.Empty, role);
    }
}
