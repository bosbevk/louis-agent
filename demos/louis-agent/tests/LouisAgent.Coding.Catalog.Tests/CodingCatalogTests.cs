using LouisAgent.Choices;
using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Delegation.Addresses;
using LouisAgent.Identities.Guide;
using LouisAgent.Identities.Triage;

namespace LouisAgent.Coding.Tests;

/// <summary>
/// The demo's catalogue runs three identities: the coding agent, first and so the default; the expert, the guide in
/// the site's corner, whose only pack is the docs (F109.3); and the triage team on the triage pack (F106.3.2).
/// </summary>
public class CodingCatalogTests
{
    /// <summary>
    /// The triage team is there by name, with its own voice, and every pack it names is one the catalogue loads.
    /// </summary>
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

    /// <summary>
    /// The manager's calls (F127.3.1) each reach one of the team's roles here, one level down, in the order of its
    /// roles.
    /// </summary>
    [Test]
    public void TheTriageManagersCalls_EachReachOneOfItsRoles()
    {
        var catalog = CodingPrompts.Catalog;
        IAgentIdentity triage = catalog.Identity(TriageIdentityName.Triage.Wire())!;

        var reached = triage.Calls
            .Select(call => AgentAddress.TryParse(call, out var address)
                ? AgentAddresses.Resolve(address, catalog.Identities, out _)
                : null)
            .ToList();

        Assert.Multiple(() =>
        {
            Assert.That(reached.Select(agent => agent?.Kind), Has.All.EqualTo(AddressKind.Specialist));
            Assert.That(reached.Select(agent => agent?.Role),
                Is.EqualTo(Enum.GetValues<TriageRole>().Select(role => role.Wire())));
        });
    }

    /// <summary>The guide is there by name, with its own voice, and names the docs, leads and site packs.</summary>
    [Test]
    public void TheExpert_IsASecondIdentity_OnTheDocsPack()
    {
        var catalog = CodingPrompts.Catalog;
        var expert = catalog.Identity(GuideIdentityName.LouisAgentGuide.Wire());

        Assert.Multiple(() =>
        {
            Assert.That(catalog.Identities[0].Name, Is.Not.EqualTo(GuideIdentityName.LouisAgentGuide.Wire()),
                "the coding agent stays the default");
            Assert.That(expert, Is.Not.Null);
            Assert.That(expert!.DefaultVoice, Is.EqualTo($"{expert.Name}/voice"));
            Assert.That(expert.Packs,
                Is.EqualTo(new[] { GuidePack.Docs.Wire(), GuidePack.Leads.Wire(), GuidePack.Site.Wire() }));
            Assert.That(catalog.Toolsets.Select(t => t.Name),
                Does.Contain(GuidePack.Docs.Wire()).And.Contain(GuidePack.Leads.Wire()));
        });
    }
}
