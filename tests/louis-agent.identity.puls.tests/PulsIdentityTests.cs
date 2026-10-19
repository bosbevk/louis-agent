using LouisAgent.Core.Agent;
using LouisAgent.Identities.Puls;

namespace LouisAgent.Identities.Puls.Tests;

// CL15 step 2: who Puls is, as the catalogue sees it.
public class PulsIdentityTests
{
    [Test]
    public void Puls_IsNamedPuls_AndFitsTheCatalogue()
    {
        var puls = new PulsIdentity();

        Assert.That(puls.Name, Is.EqualTo("puls"));
        Assert.That(puls.Packs, Is.Empty, "shops comes in step 4");
        // Step 3: its prompts are its own, embedded with their paths (the manifest lines in its csproj work).
        Assert.That(puls.Prompts.GetFileInfo("agent/puls/voice.md").Exists, Is.True, "a mode");
        Assert.That(puls.Prompts.GetFileInfo("specialists/puls-cook.md").Exists, Is.True, "a specialist");
        Assert.That(puls.Prompts.GetFileInfo("minions/puls-minion.md").Exists, Is.True, "a minion base (F30, kept)");
        var catalog = new AgentCatalog([puls], []);
        Assert.That(catalog.Identities, Is.EqualTo(new[] { puls }));
    }
}
