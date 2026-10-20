namespace LouisAgent.Core.Agent.Toolsets;

using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools.Agents;
using LouisAgent.Core.Tools.Currency;
using LouisAgent.Core.Tools.Documents;
using LouisAgent.Core.Tools.DotNet;
using LouisAgent.Core.Tools.Git;
using LouisAgent.Core.Tools.Integrations.DevOps;
using LouisAgent.Core.Tools.Integrations.Paymo;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Web.Fetching;
using LouisAgent.Core.Tools.Web.Quotes;
using LouisAgent.Core.Tools.Workspace;
using Microsoft.Extensions.Logging;

/// <summary>
/// The toolsets a session can load (F5). Nothing is loaded by default: a session names the toolsets it needs, and
/// <see cref="Resolve"/> checks each name before any tool is built.
/// </summary>
public static class ToolsetCatalog
{
    private static readonly ILogger Log = AgentLogging.For<ToolsetDefinition>();

    /// <summary>Every toolset, in the order <c>GET /toolsets</c> lists them.</summary>
    public static IReadOnlyList<ToolsetDefinition> All { get; } =
    [
        new("files", "Read, write, search, copy, rename and delete files in the workspace",
            GuideFile: SkillFiles.Guide("files"),
            MissingSettings: _ => [],
            // S (CL15): five classes, one concern each (a file's content, the tree's shape, browsing, searching, documents).
            ToolTypes: [typeof(WorkspaceFileTools), typeof(WorkspaceOrganiseTools), typeof(WorkspaceBrowseTools), typeof(WorkspaceSearchTools), typeof(DocumentTools)],
            CreateTools: ctx =>
            [
                new WorkspaceFileTools(Root(ctx)), new WorkspaceOrganiseTools(Root(ctx)), new WorkspaceBrowseTools(Root(ctx)),
                new WorkspaceSearchTools(Root(ctx)), new DocumentTools(Root(ctx)),
            ]),

        new("git", "Status, diffs, branches, staging, commits, merges and history for the workspace repository",
            GuideFile: SkillFiles.Guide("git"),
            MissingSettings: _ => [],
            ToolTypes: [typeof(GitTools)],
            CreateTools: ctx => [new GitTools(Root(ctx))]),

        new("dotnet", "Build, test, run and restore .NET projects; list packages; SDK information",
            GuideFile: SkillFiles.Guide("dotnet"),
            MissingSettings: _ => [],
            ToolTypes: [typeof(DotNetTools)],
            CreateTools: ctx => [new DotNetTools(Root(ctx))]),

        new("python", "Run Python code or scripts and manage packages in the agent's own virtual environment",
            GuideFile: SkillFiles.Guide("python"),
            MissingSettings: _ => [],
            ToolTypes: [typeof(PythonTools)],
            CreateTools: ctx => [new PythonTools(Root(ctx))]),

        new("powershell", "Run PowerShell scripts (PowerShell 7, or Windows PowerShell 5.1 on Windows)",
            GuideFile: SkillFiles.Guide("powershell"),
            MissingSettings: _ => [],
            ToolTypes: [typeof(PowerShellTools)],
            CreateTools: ctx => [new PowerShellTools(Root(ctx))]),

        new("bash", "Run bash scripts (Git Bash on Windows, /bin/bash elsewhere)",
            GuideFile: SkillFiles.Guide("bash"),
            MissingSettings: _ => [],
            ToolTypes: [typeof(BashTools)],
            CreateTools: ctx => [new BashTools(Root(ctx))]),

        new("web", "Search the web and fetch web pages",
            GuideFile: SkillFiles.Guide("web"),
            MissingSettings: _ => [],
            // S (CL15): three classes (finding and reading, a quote's provenance, currency); one guarded fetcher for the two that fetch.
            ToolTypes: [typeof(WebTools), typeof(QuoteTools), typeof(CurrencyTools)],
            CreateTools: ctx =>
            {
                var fetcher = new PageFetcher();
                return [new WebTools(ctx.WebSearch, fetcher, ctx.Renderer), new QuoteTools(fetcher, ctx.Renderer), new CurrencyTools(ctx.Rates)];
            },
            ParallelSafe: true),

        // CL15: the shops (F33, F34) are a pack, packs/louis-agent.pack.shops; a host loads it with PACKS=shops or an identity that names it.

        new("paymo", "Paymo time tracking: time entries, tasks, projects and summaries",
            GuideFile: SkillFiles.Guide("paymo"),
            MissingSettings: ctx => Missing(ctx, ("PAYMO_API_KEY", ctx.Options.PaymoApiKey)),
            ToolTypes: [typeof(PaymoTools)],
            CreateTools: ctx => [new PaymoTools(ctx.Options)]),

        new("devops", "Azure DevOps work items, sprints and team queries",
            GuideFile: SkillFiles.Guide("devops"),
            MissingSettings: ctx => Missing(ctx,
                ("DEVOPS_API_KEY", ctx.Options.DevopsApiKey),
                ("DEVOPS_ORGANIZATION", ctx.Options.DevopsOrganization),
                ("DEVOPS_PROJECT", ctx.Options.DevopsProject)),
            ToolTypes: [typeof(DevOpsTools)],
            CreateTools: ctx => [new DevOpsTools(
                ctx.Options.DevopsApiKey!, ctx.Options.DevopsOrganization!, ctx.Options.DevopsProject!, ctx.Options.DevopsTeam)]),

        new("specialists", "Hand a separate task to a specialist agent: a sub-session on a peer with only the toolsets it needs",
            GuideFile: SkillFiles.Guide("specialists"),
            MissingSettings: ctx => ctx.Options.AgentPeers.Count == 0 ? ["AGENT_PEERS"] : [],
            ToolTypes: [typeof(SpecialistTools)],
            CreateTools: ctx => [new SpecialistTools(ctx.Options, ctx.UsageSink)]),

        new("minions", "Hand one task and one of your own tools to a minion on a cheap model (a specialist's toolset, F30)",
            GuideFile: SkillFiles.Guide("minions"),
            MissingSettings: ctx => new[] { ctx.Options.AgentPeers.Count == 0 ? "AGENT_PEERS" : null, ctx.Options.MinionLlms.Count == 0 ? "MINION_LLMS" : null }
                .OfType<string>().ToList(),
            ToolTypes: [typeof(MinionTools)],
            CreateTools: ctx => [new MinionTools(ctx.Options, ctx.UsageSink)]),

        // Engine-provided: CreateSkillFile, ReloadSkills, CreateTool and ListAgentTools are registered by AgentEngine
        // when this name is in the resolved list (step 3), because they need the engine's own registry and skill set.
        new("skills", "Create and reload skills, and build and list agent-built tools",
            GuideFile: SkillFiles.Guide("self-extension"),
            MissingSettings: _ => [],
            CreateTools: null),
    ];

    // CL15: ToolNames, ToolNamesOf and ToolsetOf moved to AgentCatalog, so a pack's tools count too.

    /// <summary>
    /// Whether an engine of <paramref name="resolved"/> runs a round's tool calls at once (F34): only when it has toolsets
    /// and every one is <see cref="ToolsetDefinition.ParallelSafe"/>. No toolsets is the coding agent's default set, in order.
    /// </summary>
    public static bool RunsInParallel(IReadOnlyCollection<ToolsetDefinition> resolved) =>
        resolved.Count > 0 && resolved.All(t => t.ParallelSafe);

    /// <summary>Looks a core toolset up by name (case-insensitive); null when there is none.</summary>
    public static ToolsetDefinition? Find(string name) => Find(name, All);

    private static ToolsetDefinition? Find(string name, IReadOnlyList<ToolsetDefinition> toolsets) =>
        toolsets.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));

    /// <summary>The mark of an optional toolset (F33): <c>shops?</c> loads when this instance has it, else is skipped.</summary>
    public const char OptionalMark = '?';

    /// <summary>A toolset's name without its optional mark.</summary>
    public static string NameOf(string name) => name.TrimEnd(OptionalMark);

    private static bool IsOptional(string name) => name.EndsWith(OptionalMark);

    /// <summary>
    /// Cleans a client's list: trims, lowercases, drops empty entries and duplicates, and sorts it, so the same set
    /// always gives the same engine (the engine cache and the cached prefix both depend on that). An optional name keeps
    /// its mark; named both ways, the required one wins.
    /// </summary>
    public static IReadOnlyList<string> Normalise(IEnumerable<string>? names)
    {
        var cleaned = (names ?? [])
            .Select(n => (n ?? string.Empty).Trim().ToLowerInvariant())
            .Where(n => NameOf(n).Length > 0)
            .ToList();
        return cleaned
            .GroupBy(NameOf, StringComparer.Ordinal)
            .Select(g => g.Any(n => !IsOptional(n)) ? g.Key : g.Key + OptionalMark)
            .Order(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Resolves a client's request to core's definitions, in the order of <see cref="All"/>; a host with packs resolves
    /// through its <see cref="AgentCatalog"/>. Throws
    /// <see cref="ToolsetException"/> for an unknown name (listing the valid ones; an optional one too, so a typo is never
    /// skipped in silence) or a required toolset whose settings are missing on this instance (listing them), before any
    /// tool is built. An optional toolset (<c>shops?</c>) whose settings are missing is left out, and the log says why.
    /// </summary>
    public static IReadOnlyList<ToolsetDefinition> Resolve(IEnumerable<string>? names, ToolsetContext context) =>
        Resolve(names, context, All);

    /// <summary>The same, over <paramref name="toolsets"/>: core's and a catalogue's packs', in that order (CL15).</summary>
    internal static IReadOnlyList<ToolsetDefinition> Resolve(IEnumerable<string>? names, ToolsetContext context, IReadOnlyList<ToolsetDefinition> toolsets)
    {
        var requested = Normalise(names);

        var unknown = requested.Where(n => Find(NameOf(n), toolsets) is null).ToList();
        if (unknown.Count > 0)
            throw new ToolsetException(MessageText.Format(ErrorMessage.UnknownToolsets,
                new { Names = string.Join(", ", unknown), Valid = string.Join(", ", toolsets.Select(t => t.Name)) }));

        var optional = requested.Where(IsOptional).Select(NameOf).ToHashSet(StringComparer.Ordinal);
        var resolved = toolsets.Where(t => requested.Select(NameOf).Contains(t.Name, StringComparer.Ordinal)).ToList();

        var unavailable = resolved
            .Select(t => (Toolset: t, Missing: t.MissingSettings(context)))
            .Where(x => x.Missing.Count > 0)
            .ToList();
        foreach (var skipped in unavailable.Where(x => optional.Contains(x.Toolset.Name)))
        {
            Log.Log(LogLevel.Information, LogMessage.OptionalToolsetSkipped, new { skipped.Toolset.Name, Settings = string.Join(", ", skipped.Missing) });
            resolved.Remove(skipped.Toolset);
        }
        var refused = unavailable.Where(x => !optional.Contains(x.Toolset.Name)).ToList();
        if (refused.Count > 0)
            throw new ToolsetException(string.Join(" ", refused.Select(x => MessageText.Format(ErrorMessage.ToolsetSettingsMissing,
                new { x.Toolset.Name, Settings = string.Join(", ", x.Missing) }))));

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
