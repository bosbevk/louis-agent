using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Choices;
using LouisAgent.Core.Agent.Toolsets;

using LouisAgent.Core.Agent.Catalog;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Delegation;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Core.Agent.Toolsets;
/// <summary>
/// The toolsets a session can load (F5). Nothing is loaded by default: a session names the toolsets it needs, and
/// <see cref="Resolve"/> checks each name before any tool is built.
/// </summary>
public static class ToolsetCatalog
{
    private static readonly ILogger Log = AgentLogging.For<ToolsetDefinition>();

    /// <summary>Every toolset, in the order <c>GET /v1/toolsets</c> lists them.</summary>
    public static IReadOnlyList<ToolsetDefinition> All { get; } =
    [
        new(CoreToolset.Specialists.Wire(),
            "Hand a separate task to a specialist agent: a sub-session on a peer with only the toolsets it needs",
            GuideFile: SkillFiles.Guide(CoreToolset.Specialists.Wire()),
            MissingSettings: ctx => ctx.Options.AgentPeers.Count == 0 ? ["AGENT_PEERS"] : [],
            ToolTypes: [typeof(SpecialistTools)],
            CreateTools: ctx => [new SpecialistTools(ctx.Options, ctx.UsageSink)]),

        new(CoreToolset.Minions.Wire(),
            "Hand one task and one of your own tools to a minion on a cheap model (a specialist's toolset, F30)",
            GuideFile: SkillFiles.Guide(CoreToolset.Minions.Wire()),
            MissingSettings: ctx => new[]
                {
                    ctx.Options.AgentPeers.Count == 0 ? "AGENT_PEERS" : null,
                    ctx.Options.MinionLlms.Count == 0 ? "MINION_LLMS" : null
                }
                .OfType<string>().ToList(),
            ToolTypes: [typeof(MinionTools)],
            CreateTools: ctx => [new MinionTools(ctx.Options, ctx.UsageSink)]),

        // Engine-provided: CreateSkillFile, ReloadSkills, CreateTool and ListAgentTools are registered by AgentEngine
        // when this name is in the resolved list (step 3), because they need the engine's own registry and skill set.
        new(CoreToolset.Skills.Wire(), "Create and reload skills, and build and list agent-built tools",
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
    internal static IReadOnlyList<ToolsetDefinition> Resolve(IEnumerable<string>? names, ToolsetContext context,
        IReadOnlyList<ToolsetDefinition> toolsets)
    {
        var requested = Normalise(names);

        var unknown = requested.Where(n => Find(NameOf(n), toolsets) is null).ToList();
        if (unknown.Count > 0)
            throw new ToolsetException(MessageText.Format(AgentErrorMessage.UnknownToolsets,
                new { Names = string.Join(", ", unknown), Valid = string.Join(", ", toolsets.Select(t => t.Name)) }));

        var optional = requested.Where(IsOptional).Select(NameOf).ToHashSet(StringComparer.Ordinal);
        var resolved = toolsets.Where(t => requested.Select(NameOf).Contains(t.Name, StringComparer.Ordinal)).ToList();

        var unavailable = resolved
            .Select(t => (Toolset: t, Missing: t.MissingSettings(context)))
            .Where(x => x.Missing.Count > 0)
            .ToList();
        foreach (var skipped in unavailable.Where(x => optional.Contains(x.Toolset.Name)))
        {
            Log.Log(LogLevel.Information, AgentLogMessage.OptionalToolsetSkipped,
                new { skipped.Toolset.Name, Settings = string.Join(", ", skipped.Missing) });
            resolved.Remove(skipped.Toolset);
        }

        var refused = unavailable.Where(x => !optional.Contains(x.Toolset.Name)).ToList();
        if (refused.Count > 0)
            throw new ToolsetException(string.Join(" ", refused.Select(x =>
                MessageText.Format(AgentErrorMessage.ToolsetSettingsMissing,
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
        resolved.Where(t => t.GuideFile is not null).Select(t => t.GuideFile!).Distinct(StringComparer.Ordinal)
            .ToList();

    private static string Root(ToolsetContext context) => Path.GetFullPath(context.Options.WorkspaceRoot);

    private static IReadOnlyList<string> Missing(ToolsetContext context,
        params (string Setting, string? Value)[] settings) =>
        settings.Where(s => string.IsNullOrWhiteSpace(s.Value)).Select(s => s.Setting).ToList();
}
