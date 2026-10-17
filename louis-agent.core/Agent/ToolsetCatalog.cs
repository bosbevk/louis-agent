namespace LouisAgent.Core.Agent;

using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.DotNet;
using LouisAgent.Core.Tools.Git;
using LouisAgent.Core.Tools.Integrations.DevOps;
using LouisAgent.Core.Tools.Integrations.Paymo;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Tools.Shops;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Workspace;

/// <summary>
/// The toolsets a session can load (F5). Nothing is loaded by default: a session names the toolsets it needs, and
/// <see cref="Resolve"/> checks each name before any tool is built.
/// </summary>
public static class ToolsetCatalog
{
    /// <summary>Every toolset, in the order <c>GET /toolsets</c> lists them.</summary>
    public static IReadOnlyList<ToolsetDefinition> All { get; } =
    [
        new("files", "Read, write, search, copy, rename and delete files in the workspace",
            GuideFile: SkillFiles.Guide("files"),
            MissingSettings: _ => [],
            ToolType: typeof(WorkspaceTools),
            CreateTools: ctx => [new WorkspaceTools(Root(ctx))]),

        new("git", "Status, diffs, branches, staging, commits, merges and history for the workspace repository",
            GuideFile: SkillFiles.Guide("git"),
            MissingSettings: _ => [],
            ToolType: typeof(GitTools),
            CreateTools: ctx => [new GitTools(Root(ctx))]),

        new("dotnet", "Build, test, run and restore .NET projects; list packages; SDK information",
            GuideFile: SkillFiles.Guide("dotnet"),
            MissingSettings: _ => [],
            ToolType: typeof(DotNetTools),
            CreateTools: ctx => [new DotNetTools(Root(ctx))]),

        new("python", "Run Python code or scripts and manage packages in the agent's own virtual environment",
            GuideFile: SkillFiles.Guide("python"),
            MissingSettings: _ => [],
            ToolType: typeof(PythonTools),
            CreateTools: ctx => [new PythonTools(Root(ctx))]),

        new("powershell", "Run PowerShell scripts (PowerShell 7, or Windows PowerShell 5.1 on Windows)",
            GuideFile: SkillFiles.Guide("powershell"),
            MissingSettings: _ => [],
            ToolType: typeof(PowerShellTools),
            CreateTools: ctx => [new PowerShellTools(Root(ctx))]),

        new("bash", "Run bash scripts (Git Bash on Windows, /bin/bash elsewhere)",
            GuideFile: SkillFiles.Guide("bash"),
            MissingSettings: _ => [],
            ToolType: typeof(BashTools),
            CreateTools: ctx => [new BashTools(Root(ctx))]),

        new("web", "Search the web and fetch web pages",
            GuideFile: SkillFiles.Guide("web"),
            MissingSettings: _ => [],
            ToolType: typeof(WebTools),
            CreateTools: ctx => [new WebTools(ctx.WebSearch, rates: ctx.Rates, renderer: ctx.Renderer)]),

        // F33: a shop's products from its own catalogue, through the guarded browser; off unless SHOP_CATALOGUES names a
        // shop (development and benchmarks first, ADR 0023).
        new("shops", "A shop's products and prices from its own catalogue (Checkers)",
            GuideFile: SkillFiles.Guide("shops"),
            MissingSettings: ctx => Missing(ctx,
                ("SHOP_CATALOGUES", ShopCatalogueFactory.Enabled(ctx.Options.ShopCatalogues).Count > 0 ? "set" : null),
                ("BROWSER_URL", ctx.Browser is null ? null : "set")),
            ToolType: typeof(ShopTools),
            CreateTools: ctx => [new ShopTools(ShopCatalogueFactory.Create(ShopCatalogueFactory.Enabled(ctx.Options.ShopCatalogues), ctx.Browser!))]),

        new("paymo", "Paymo time tracking: time entries, tasks, projects and summaries",
            GuideFile: SkillFiles.Guide("paymo"),
            MissingSettings: ctx => Missing(ctx, ("PAYMO_API_KEY", ctx.Options.PaymoApiKey)),
            ToolType: typeof(PaymoTools),
            CreateTools: ctx => [new PaymoTools(ctx.Options)]),

        new("devops", "Azure DevOps work items, sprints and team queries",
            GuideFile: SkillFiles.Guide("devops"),
            MissingSettings: ctx => Missing(ctx,
                ("DEVOPS_API_KEY", ctx.Options.DevopsApiKey),
                ("DEVOPS_ORGANIZATION", ctx.Options.DevopsOrganization),
                ("DEVOPS_PROJECT", ctx.Options.DevopsProject)),
            ToolType: typeof(DevOpsTools),
            CreateTools: ctx => [new DevOpsTools(
                ctx.Options.DevopsApiKey!, ctx.Options.DevopsOrganization!, ctx.Options.DevopsProject!, ctx.Options.DevopsTeam)]),

        new("specialists", "Hand a separate task to a specialist agent: a sub-session on a peer with only the toolsets it needs",
            GuideFile: SkillFiles.Guide("specialists"),
            MissingSettings: ctx => ctx.Options.AgentPeers.Count == 0 ? ["AGENT_PEERS"] : [],
            ToolType: typeof(SpecialistTools),
            CreateTools: ctx => [new SpecialistTools(ctx.Options, ctx.UsageSink)]),

        new("minions", "Hand one task and one of your own tools to a minion on a cheap model (a specialist's toolset, F30)",
            GuideFile: SkillFiles.Guide("minions"),
            MissingSettings: ctx => new[] { ctx.Options.AgentPeers.Count == 0 ? "AGENT_PEERS" : null, ctx.Options.MinionLlms.Count == 0 ? "MINION_LLMS" : null }
                .OfType<string>().ToList(),
            ToolType: typeof(MinionTools),
            CreateTools: ctx => [new MinionTools(ctx.Options, ctx.UsageSink)]),

        // Engine-provided: CreateSkillFile, ReloadSkills, CreateTool and ListAgentTools are registered by AgentEngine
        // when this name is in the resolved list (step 3), because they need the engine's own registry and skill set.
        new("skills", "Create and reload skills, and build and list agent-built tools",
            GuideFile: SkillFiles.Guide("self-extension"),
            MissingSettings: _ => [],
            CreateTools: null),
    ];

    /// <summary>
    /// Every tool name a catalogue toolset can register, loaded in this engine or not: an agent-built tool may not take
    /// one, or it would collide when a session loads that toolset.
    /// </summary>
    public static IReadOnlySet<string> ToolNames { get; } = All
        .Where(t => t.ToolType is not null)
        .SelectMany(t => ToolRegistry.ToolMethods(t.ToolType!))
        .Select(m => m.Name)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The tool names <paramref name="resolved"/> register (an engine-provided toolset contributes none): what a minion's
    /// one tool may be chosen from (F30).
    /// </summary>
    public static IReadOnlySet<string> ToolNamesOf(IEnumerable<ToolsetDefinition> resolved) => resolved
        .Where(t => t.ToolType is not null)
        .SelectMany(t => ToolRegistry.ToolMethods(t.ToolType!))
        .Select(m => m.Name)
        .ToHashSet(StringComparer.Ordinal);

    /// <summary>The toolset whose class builds <paramref name="tool"/> (a minion's one tool names its toolset, F30); null for none.</summary>
    public static string? ToolsetOf(string tool) => All
        .FirstOrDefault(t => t.ToolType is not null && ToolRegistry.ToolMethods(t.ToolType).Any(m => string.Equals(m.Name, tool, StringComparison.Ordinal)))?.Name;

    /// <summary>Looks a toolset up by name (case-insensitive); null when there is none.</summary>
    public static ToolsetDefinition? Find(string name) =>
        All.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Cleans a client's list: trims, lowercases, drops empty entries and duplicates, and sorts it, so the same set
    /// always gives the same engine (the engine cache and the cached prefix both depend on that).
    /// </summary>
    public static IReadOnlyList<string> Normalise(IEnumerable<string>? names) =>
        (names ?? [])
            .Select(n => (n ?? string.Empty).Trim().ToLowerInvariant())
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Resolves a client's request to definitions, in the order of <see cref="All"/>. Throws
    /// <see cref="ToolsetException"/> for an unknown name (listing the valid ones) or a toolset whose settings are
    /// missing on this instance (listing them), before any tool is built.
    /// </summary>
    public static IReadOnlyList<ToolsetDefinition> Resolve(IEnumerable<string>? names, ToolsetContext context)
    {
        var requested = Normalise(names);

        var unknown = requested.Where(n => Find(n) is null).ToList();
        if (unknown.Count > 0)
            throw new ToolsetException(MessageText.Format(ErrorMessage.UnknownToolsets,
                new { Names = string.Join(", ", unknown), Valid = string.Join(", ", All.Select(t => t.Name)) }));

        var resolved = All.Where(t => requested.Contains(t.Name, StringComparer.Ordinal)).ToList();

        var unavailable = resolved
            .Select(t => (t.Name, Missing: t.MissingSettings(context)))
            .Where(x => x.Missing.Count > 0)
            .ToList();
        if (unavailable.Count > 0)
            throw new ToolsetException(string.Join(" ", unavailable.Select(x => MessageText.Format(ErrorMessage.ToolsetSettingsMissing,
                new { x.Name, Settings = string.Join(", ", x.Missing) }))));

        return resolved;
    }

    /// <summary>The tool objects for resolved toolsets. Engine-provided toolsets contribute nothing here.</summary>
    public static IReadOnlyList<object> BuildTools(IEnumerable<ToolsetDefinition> resolved, ToolsetContext context) =>
        resolved
            .Where(t => t.CreateTools is not null)
            .SelectMany(t => t.CreateTools!(context))
            .ToList();

    /// <summary>The guides the resolved toolsets need, in catalogue order.</summary>
    public static IReadOnlyList<string> GuideFiles(IEnumerable<ToolsetDefinition> resolved) =>
        resolved.Where(t => t.GuideFile is not null).Select(t => t.GuideFile!).Distinct(StringComparer.Ordinal).ToList();

    private static string Root(ToolsetContext context) => Path.GetFullPath(context.Options.WorkspaceRoot);

    private static IReadOnlyList<string> Missing(ToolsetContext context, params (string Setting, string? Value)[] settings) =>
        settings.Where(s => string.IsNullOrWhiteSpace(s.Value)).Select(s => s.Setting).ToList();
}
