using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Hosting.EnvFiles;
using LouisAgent.Identities.Coding;
using LouisAgent.Identities.Expert;
using LouisAgent.Packs.Docs;
using LouisAgent.Packs.Leads;
using LouisAgent.Packs.Site;
using LouisAgent.Packs.DevOps;
using LouisAgent.Packs.Web;
using LouisAgent.Packs.Shell;
using LouisAgent.Packs.Files;
using LouisAgent.Packs.Git;
using LouisAgent.Packs.DotNet;
using LouisAgent.Packs.Paymo;

namespace LouisAgent.Coding;

/// <summary>What the coding agent can do: every program of this demo starts from this one list.</summary>
/// <remarks>
/// Composition Root: this demo says what it loads, and the framework's hosts just take it. It's a class of its own only so the
/// demo's four programs and its golden files share one list.
/// </remarks>
public static class CodingCatalog
{
    /// <summary>The packs, with the settings each reads, so the settings files load first.</summary>
    public static IReadOnlyList<IAgentPack> Packs()
    {
        HostEnvironment.Load();
        return Packs(PaymoOptions.FromEnvironment(), DevOpsOptions.FromEnvironment(), DocsOptions.FromEnvironment(),
            LeadsOptions.FromEnvironment());
    }

    /// <summary>The same list with the settings given, so the golden files hold it without this machine's keys.</summary>
    public static IReadOnlyList<IAgentPack> Packs(PaymoOptions paymo, DevOpsOptions devOps, DocsOptions docs, LeadsOptions leads) =>
    [
        new FilesPack(), new GitPack(), new DotNetPack(), new ShellPack(), new WebPack(), new PaymoPack(paymo),
        new DevOpsPack(devOps), new DocsPack(docs), new LeadsPack(leads), new SitePack(),
    ];

    /// <summary>
    /// The catalogue a program hands to its host: the coding agent first, so it is the default, and the expert
    /// beside it (F109.3), which a session opens as by name.
    /// </summary>
    public static AgentCatalog Create() => new([new CodingIdentity(), new ExpertIdentity()], Packs());
}
