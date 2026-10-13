namespace LouisAgent.Core.Agent;

using LouisAgent.Core.Config;
using LouisAgent.Core.Tools.DotNet;
using LouisAgent.Core.Tools.Git;
using LouisAgent.Core.Tools.Integrations.DevOps;
using LouisAgent.Core.Tools.Integrations.Paymo;
using LouisAgent.Core.Tools.Shell;
using LouisAgent.Core.Tools.Web;
using LouisAgent.Core.Tools.Workspace;

/// <summary>What a toolset needs to build its tools for one engine.</summary>
public sealed record ToolsetContext(AgentOptions Options, IWebSearchProvider WebSearch);

/// <summary>
/// One named toolset: what it is, which settings it needs on this instance, its guide, and how to build its tools.
/// </summary>
/// <param name="Name">The name clients send in <c>toolsets</c>, lowercase.</param>
/// <param name="Description">One line, shown by <c>GET /toolsets</c>.</param>
/// <param name="GuideFile">
/// The skill file that guides the model on these tools, or null when the guide is a section of <c>default.md</c>.
/// </param>
/// <param name="MissingSettings">Settings this instance lacks for the toolset; empty when it can be used.</param>
/// <param name="CreateTools">
/// Builds the tool objects for an engine (its public methods become tools). Null when the engine provides the tools
/// itself (<c>skills</c>, see <see cref="AgentEngine"/>).
/// </param>
public sealed record ToolsetDefinition(
    string Name,
    string Description,
    string? GuideFile,
    Func<ToolsetContext, IReadOnlyList<string>> MissingSettings,
    Func<ToolsetContext, IReadOnlyList<object>>? CreateTools)
{
    /// <summary>True when the engine adds these tools itself rather than from <see cref="CreateTools"/>.</summary>
    public bool IsEngineProvided => CreateTools is null;

    /// <summary>True when this instance has every setting the toolset needs.</summary>
    public bool IsAvailable(ToolsetContext context) => MissingSettings(context).Count == 0;
}

/// <summary>A toolset request that can't be honoured: an unknown name, or a toolset whose settings are missing.</summary>
public sealed class ToolsetException(string message) : Exception(message);

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
            GuideFile: null,
            MissingSettings: _ => [],
            CreateTools: ctx => [new WorkspaceTools(Root(ctx))]),

        new("git", "Status, diffs, branches, staging, commits, merges and history for the workspace repository",
            GuideFile: null,
            MissingSettings: _ => [],
            CreateTools: ctx => [new GitTools(Root(ctx))]),

        new("dotnet", "Build, test, run and restore .NET projects; list packages; SDK information",
            GuideFile: "dotnet-skills.md",
            MissingSettings: _ => [],
            CreateTools: ctx => [new DotNetTools(Root(ctx))]),

        new("python", "Run Python code or scripts and manage packages in the agent's own virtual environment",
            GuideFile: "python-skills.md",
            MissingSettings: _ => [],
            CreateTools: ctx => [new PythonTools(Root(ctx))]),

        new("powershell", "Run PowerShell scripts (PowerShell 7, or Windows PowerShell 5.1 on Windows)",
            GuideFile: "powershell-skills.md",
            MissingSettings: _ => [],
            CreateTools: ctx => [new PowerShellTools(Root(ctx))]),

        new("bash", "Run bash scripts (Git Bash on Windows, /bin/bash elsewhere)",
            GuideFile: "bash-skills.md",
            MissingSettings: _ => [],
            CreateTools: ctx => [new BashTools(Root(ctx))]),

        new("web", "Search the web and fetch web pages",
            GuideFile: "web-skills.md",
            MissingSettings: _ => [],
            CreateTools: ctx => [new WebTools(ctx.WebSearch)]),

        new("paymo", "Paymo time tracking: time entries, tasks, projects and summaries",
            GuideFile: "paymo-skills.md",
            MissingSettings: ctx => Missing(ctx, ("PAYMO_API_KEY", ctx.Options.PaymoApiKey)),
            CreateTools: ctx => [new PaymoTools(ctx.Options)]),

        new("devops", "Azure DevOps work items, sprints and team queries",
            GuideFile: "devops-skills.md",
            MissingSettings: ctx => Missing(ctx,
                ("DEVOPS_API_KEY", ctx.Options.DevopsApiKey),
                ("DEVOPS_ORGANIZATION", ctx.Options.DevopsOrganization),
                ("DEVOPS_PROJECT", ctx.Options.DevopsProject)),
            CreateTools: ctx => [new DevOpsTools(
                ctx.Options.DevopsApiKey!, ctx.Options.DevopsOrganization!, ctx.Options.DevopsProject!, ctx.Options.DevopsTeam)]),

        // Engine-provided: CreateSkillFile, ReloadSkills, CreateTool and ListAgentTools are registered by AgentEngine
        // when this name is in the resolved list (step 3), because they need the engine's own registry and skill set.
        new("skills", "Create and reload skills, and build and list agent-built tools",
            GuideFile: "self-extension-skills.md",
            MissingSettings: _ => [],
            CreateTools: null),
    ];

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
            throw new ToolsetException(
                $"Unknown toolset(s): {string.Join(", ", unknown)}. Valid names: {string.Join(", ", All.Select(t => t.Name))}.");

        var resolved = All.Where(t => requested.Contains(t.Name, StringComparer.Ordinal)).ToList();

        var unavailable = resolved
            .Select(t => (t.Name, Missing: t.MissingSettings(context)))
            .Where(x => x.Missing.Count > 0)
            .ToList();
        if (unavailable.Count > 0)
            throw new ToolsetException(string.Join(" ", unavailable.Select(x =>
                $"Toolset '{x.Name}' needs {string.Join(", ", x.Missing)} to be set on this instance.")));

        return resolved;
    }

    /// <summary>The tool objects for resolved toolsets. Engine-provided toolsets contribute nothing here.</summary>
    public static IReadOnlyList<object> BuildTools(IEnumerable<ToolsetDefinition> resolved, ToolsetContext context) =>
        resolved
            .Where(t => t.CreateTools is not null)
            .SelectMany(t => t.CreateTools!(context))
            .ToList();

    /// <summary>The skill files the resolved toolsets need. Sections of <c>default.md</c> are not listed here.</summary>
    public static IReadOnlyList<string> GuideFiles(IEnumerable<ToolsetDefinition> resolved) =>
        resolved.Where(t => t.GuideFile is not null).Select(t => t.GuideFile!).Distinct(StringComparer.Ordinal).ToList();

    private static string Root(ToolsetContext context) => Path.GetFullPath(context.Options.WorkspaceRoot);

    private static IReadOnlyList<string> Missing(ToolsetContext context, params (string Setting, string? Value)[] settings) =>
        settings.Where(s => string.IsNullOrWhiteSpace(s.Value)).Select(s => s.Setting).ToList();
}
