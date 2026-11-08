using LouisAgent.Choices;
using LouisAgent.Identities.Expert;

namespace LouisAgent.Coding.Tests;

/// <summary>
/// The demo's catalogue runs two identities (F109.3): the coding agent, first and so the default, and the expert, the
/// guide in the site's corner, whose only pack is the docs.
/// </summary>
public class CodingCatalogTests
{
    /// <summary>The expert is there by name, with its own voice, and names the docs pack and nothing else.</summary>
    [Test]
    public void TheExpert_IsASecondIdentity_OnTheDocsPack()
    {
        var catalog = CodingPrompts.Catalog;
        var expert = catalog.Identity(ExpertIdentityName.LouisAgentExpert.Wire());

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Identities[0].Name, Is.Not.EqualTo(ExpertIdentityName.LouisAgentExpert.Wire()),
                "the coding agent stays the default");
            Assert.That(expert, Is.Not.Null);
            Assert.That(expert!.DefaultVoice, Is.EqualTo($"{expert.Name}/voice"));
            Assert.That(expert.Packs, Is.EqualTo(new[] { ExpertPack.Docs.Wire() }));
            Assert.That(catalog.Toolsets.Select(t => t.Name), Does.Contain(ExpertPack.Docs.Wire()));
        });
    }
}
