using LouisAgent.Choices;
using LouisAgent.Identities.Expert;
using LouisAgent.Identities.Triage;

namespace LouisAgent.Coding.Tests;

/// <summary>
/// The demo's catalogue runs three identities: the coding agent, first and so the default; the expert, the guide in
/// the site's corner, whose only pack is the docs (F109.3); and the triage team on the triage pack (F106.3.2).
/// </summary>
public class CodingCatalogTests
{
    /// <summary>The triage team is there by name, with its own voice, and every pack it names is one the catalogue loads.</summary>
    [Test]
    public void TheTriageTeam_IsAThirdIdentity_OnPacksTheCatalogueLoads()
    {
        var catalog = CodingPrompts.Catalog;
        var triage = catalog.Identity(TriageIdentityName.Triage.Wire());
        var loaded = catalog.Packs.Select(pack => pack.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Multiple(() =>
        {
            Assert.That(triage, Is.Not.Null);
            Assert.That(triage!.DefaultVoice, Is.EqualTo($"{triage.Name}/voice"));
            Assert.That(triage.Packs.Where(pack => !loaded.Contains(pack)), Is.Empty,
                "a pack the team names and nobody loads");
            Assert.That(catalog.Toolsets.Select(t => t.Name), Does.Contain(TriageIdentityPack.Triage.Wire()));
        });
    }

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
