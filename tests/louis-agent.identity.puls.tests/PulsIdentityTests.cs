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
        var catalog = new AgentCatalog([puls], []);
        Assert.That(catalog.Identities, Is.EqualTo(new[] { puls }));
    }
}
