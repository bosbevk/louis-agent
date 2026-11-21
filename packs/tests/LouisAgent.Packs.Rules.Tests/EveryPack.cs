using LouisAgent.Composition.Packs;
using LouisAgent.Packs.Calculator;
using LouisAgent.Packs.DevOps;
using LouisAgent.Packs.Docs;
using LouisAgent.Packs.DotNet;
using LouisAgent.Packs.Files;
using LouisAgent.Packs.Git;
using LouisAgent.Packs.Leads;
using LouisAgent.Packs.Paymo;
using LouisAgent.Packs.Shell;
using LouisAgent.Packs.Site;
using LouisAgent.Packs.Triage;
using LouisAgent.Packs.Web;

namespace LouisAgent.Packs.Rules.Tests;

/// <summary>
/// Every pack in <c>packs/tools</c>, built with no settings, since the rules here read shapes and never call a tool.
/// <see cref="EveryPackTests"/> holds the list to the folders, so a new pack can't slip past the rules.
/// </summary>
public static class EveryPack
{
    /// <summary>The packs, one of each.</summary>
    public static IReadOnlyList<IAgentPack> All { get; } =
    [
        new CalculatorPack(), new DevOpsPack(new DevOpsOptions()), new DocsPack(new DocsOptions()), new DotNetPack(),
        new FilesPack(), new GitPack(), new LeadsPack(new LeadsOptions()), new PaymoPack(new PaymoOptions()),
        new ShellPack(), new SitePack(), new TriagePack(new TriageOptions()), new WebPack()
    ];

    /// <summary>The pack of this name.</summary>
    public static IAgentPack Named(string name) => All.Single(pack => pack.Name == name);
}
