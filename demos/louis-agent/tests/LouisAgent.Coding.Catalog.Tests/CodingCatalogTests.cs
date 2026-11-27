using LouisAgent.Choices;
using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Toolsets;
using LouisAgent.Core.Delegation.Addresses;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Prompting;
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
    /// The coding agent's voice names the toolsets it works with, as the triage team's does (ADR 0025), so a chat
    /// opened for it gets them, an eval's included, and only the optional ones an instance lacks are left out.
    /// </summary>
    [Test]
    public void TheCodingAgentsVoice_NamesItsToolsets()
    {
        var coding = CodingPrompts.Catalog.Identities[0];

        var toolsets = SkillLoader.ToolsetsOf(coding.DefaultVoice, RoleLevel.Agent);

        // The packs' names as the voice writes them, core's two through their enum.
        string[] named =
        [
            "files", "git", "dotnet", "python?", "powershell?", "bash?", "web?", "docs?",
            CoreToolset.Agents.Wire(), CoreToolset.Skills.Wire()
        ];
        Assert.That(toolsets, Is.EqualTo(named));
    }

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
    /// The manager's calls (F127.3.1) each reach one of the team's subagents here, one level down, named by the call
    /// itself (ADR 0133).
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
            Assert.That(reached.Select(agent => agent?.Kind), Has.All.EqualTo(AddressKind.Subagent));
            Assert.That(reached.Select(agent => agent?.Role), Is.EqualTo(triage.Calls));
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
