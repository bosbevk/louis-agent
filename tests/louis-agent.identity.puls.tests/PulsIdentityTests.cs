using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Identities.Puls;
using LouisAgent.Packs.Shops;

namespace LouisAgent.Identities.Puls.Tests;

// CL15 step 2: who Puls is, as the catalogue sees it.
public class PulsIdentityTests
{
    [Test]
    public void Puls_IsNamedPuls_AndFitsTheCatalogue()
    {
        var puls = new PulsIdentity();

        Assert.That(puls.Name, Is.EqualTo(PulsIdentityName.Puls.Wire()));
        Assert.That(puls.Packs, Is.EqualTo(new[] { PulsPack.Shops.Wire() }),
            "step 4: the cook's shops are a pack Puls names, in the identity's own enum (ADR 0043)");
        // Step 3: its prompts are its own, embedded with their paths (the manifest lines in its csproj work).
        Assert.That(puls.Prompts.GetFileInfo("agent/puls/voice.md").Exists, Is.True, "a mode");
        Assert.That(puls.Prompts.GetFileInfo("specialists/puls-cook.md").Exists, Is.True, "a specialist");
        Assert.That(puls.Prompts.GetFileInfo("minions/puls-minion.md").Exists, Is.True, "a minion base (F30, kept)");
        // The catalogue refuses an identity whose pack isn't there; with the pack, both are in.
        Assert.Throws<AgentCatalogException>(() => new AgentCatalog([puls], []), "shops named, none registered");
        var catalog = new AgentCatalog([puls], [new ShopsPack(new ShopsOptions())]);
        Assert.That(catalog.Identities, Is.EqualTo(new[] { puls }));
        Assert.That(catalog.Toolsets.Select(t => t.Name), Does.Contain(ShopsToolset.Shops.Wire()));
    }
}
