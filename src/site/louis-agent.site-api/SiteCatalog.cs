using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Hosting.EnvFiles;
using LouisAgent.Identities.Guide;
using LouisAgent.Packs.Docs;
using LouisAgent.Packs.Leads;
using LouisAgent.Packs.Site;

namespace LouisAgent.SiteApi;

/// <summary>
/// What the framework's site can do (F124): the guide in its corner, and the docs, leads and site packs it answers
/// with. Nothing else, so the public site reaches no shell, file or git tool.
/// </summary>
/// <remarks>
/// Composition Root, as the coding agent's catalogue is: the program says what it loads, and the framework's host
/// just takes it. A class of its own so the program and its test share one list.
/// </remarks>
public static class SiteCatalog
{
    /// <summary>The identities the site opens as: the guide alone, so it is the default.</summary>
    public static IReadOnlyList<IAgentIdentity> Identities { get; } = [new GuideIdentity()];

    /// <summary>The packs, with the settings each reads, so the settings files load first.</summary>
    public static IReadOnlyList<IAgentPack> Packs()
    {
        HostEnvironment.Load();
        return Packs(DocsOptions.FromEnvironment(), LeadsOptions.FromEnvironment());
    }

    /// <summary>The same packs with the settings given, so a test holds them without this PC's keys.</summary>
    public static IReadOnlyList<IAgentPack> Packs(DocsOptions docs, LeadsOptions leads) =>
        [new DocsPack(docs), new LeadsPack(leads), new SitePack()];

    /// <summary>The catalogue the program hands to its host.</summary>
    public static AgentCatalog Create() => new(Identities, Packs());
}
