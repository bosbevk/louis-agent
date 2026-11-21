using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Hosting.EnvFiles;
using LouisAgent.Identities.Framework;
using LouisAgent.Packs.Docs;
using LouisAgent.Packs.Site;

namespace LouisAgent.SiteFrameworkApi;

/// <summary>
/// What the framework's site can do (F164.2): the framework Orbi, and the docs and site packs it answers with. Nothing
/// else, so a visitor reaches no shell, no file or git tool, and no lead.
/// </summary>
/// <remarks>
/// Composition Root, as louis-agent.dev's catalogue is: the program says what it loads, and the framework's host just
/// takes it. A class of its own so the program and its test share one list.
/// </remarks>
public static class FrameworkCatalog
{
    /// <summary>The one identity the site opens as, so it is the default and the one open to visitors.</summary>
    public static IReadOnlyList<IAgentIdentity> Identities { get; } = [new FrameworkIdentity()];

    /// <summary>The packs, with the settings each reads, so the settings files load first.</summary>
    public static IReadOnlyList<IAgentPack> Packs()
    {
        HostEnvironment.Load();
        return Packs(DocsOptions.FromEnvironment());
    }

    /// <summary>The packs on the settings given: the docs it answers from, and the site it shows places on.</summary>
    public static IReadOnlyList<IAgentPack> Packs(DocsOptions docs) => [new DocsPack(docs), new SitePack()];

    /// <summary>The catalogue the program hands the framework's host.</summary>
    public static AgentCatalog Create() => new(Identities, Packs());
}
