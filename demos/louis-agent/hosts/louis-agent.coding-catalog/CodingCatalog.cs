using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Hosting.EnvFiles;
using LouisAgent.Packs.DevOps;
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
        return Packs(PaymoOptions.FromEnvironment(), DevOpsOptions.FromEnvironment());
    }

    /// <summary>The same list with the settings given, so the golden files hold it without this machine's keys.</summary>
    public static IReadOnlyList<IAgentPack> Packs(PaymoOptions paymo, DevOpsOptions devOps) =>
        [new DotNetPack(), new GitPack(), new PaymoPack(paymo), new DevOpsPack(devOps)];

    /// <summary>The catalogue a program hands to its host.</summary>
    public static AgentCatalog Create() => new([], Packs());
}
