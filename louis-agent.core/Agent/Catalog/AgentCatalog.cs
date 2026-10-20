namespace LouisAgent.Core.Agent.Catalog;

using LouisAgent.Core.Agent.Toolsets;
using System.ComponentModel;
using System.Reflection;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Tools.Clock;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileProviders;

/// <summary>
/// The one catalogue of what this instance can load (CL15, ADR 0027): core's own toolsets (<see cref="ToolsetCatalog"/>)
/// and the packs', with the identities that name them. Built once by the host from the identities and packs it was
/// given, checked as it is built (a name or prompt path in two places, a pack nobody registered, a <c>Requires</c>
/// cycle, a tool name in two toolsets or taking the engine's own), and passed down to the engine factory and the
/// sessions; no global state, so tests build one with a fake pack. <see cref="CoreOnly"/> is the catalogue of a host
/// given nothing: everything as before the cut.
/// </summary>
public sealed class AgentCatalog
{
    /// <summary>
    /// The tools the engine registers itself, whatever the toolsets (<c>AgentEngine</c>): <c>ExecuteSkill</c>, the clock,
    /// and the self-extension tools. A pack's tool may not take one of their names.
    /// </summary>
    private static readonly IReadOnlySet<string> EngineToolNames = new HashSet<string>(
        new[] { nameof(AgentEngine.ExecuteSkill), nameof(AgentEngine.CreateSkillFile), nameof(AgentEngine.ReloadSkills), nameof(AgentEngine.CreateTool), nameof(AgentEngine.ListAgentTools) }
            .Concat(ToolMethodsOfType(typeof(ClockTools)).Select(m => m.Name)),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>A host given no identity and no pack: core's toolsets and prompts, as before the cut.</summary>
    public static AgentCatalog CoreOnly { get; } = new([], []);

    private readonly Dictionary<string, ToolsetDefinition> _toolsetOfTool = new(StringComparer.Ordinal);

    public AgentCatalog(IReadOnlyList<IAgentIdentity> identities, IReadOnlyList<IAgentPack> packs)
    {
        Identities = identities.ToList();
        Packs = packs.ToList();
        CheckUnique(identities.Select(i => i.Name), "identity");
        CheckUnique(packs.Select(p => p.Name), "pack");

        Toolsets = ToolsetCatalog.All.Concat(packs.SelectMany(p => p.Toolsets)).ToList();
        CheckUnique(Toolsets.Select(t => t.Name), "toolset");

        var registered = packs.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var identity in identities) CheckNamed(identity.Name, identity.Packs, registered);
        foreach (var pack in packs) CheckNamed(pack.Name, pack.Requires, registered);
        CheckNoCycle(packs);

        Prompts = identities.Select(i => i.Prompts).Concat(packs.Select(p => p.Prompts)).ToList();
        CheckUnique(SkillFileSource.CoreFiles.Concat(Prompts.SelectMany(SkillFileSource.FilesOf)), "prompt path");

        foreach (var toolset in Toolsets)
            foreach (string name in NamesOf(toolset))
            {
                if (EngineToolNames.Contains(name))
                    throw new AgentCatalogException(MessageText.Format(ErrorMessage.CatalogToolNameIsTheEngines, new { Tool = name, Toolset = toolset.Name }));
                if (_toolsetOfTool.Keys.FirstOrDefault(k => string.Equals(k, name, StringComparison.OrdinalIgnoreCase)) is { } taken)
                    throw new AgentCatalogException(MessageText.Format(ErrorMessage.CatalogToolNameTwice, new { Tool = name, First = _toolsetOfTool[taken].Name, Second = toolset.Name }));
                _toolsetOfTool[name] = toolset;
            }
        ToolNames = _toolsetOfTool.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Core's toolsets first, in their order, then each pack's.</summary>
    public IReadOnlyList<ToolsetDefinition> Toolsets { get; }

    public IReadOnlyList<IAgentIdentity> Identities { get; }

    public IReadOnlyList<IAgentPack> Packs { get; }

    /// <summary>The identities' and packs' prompts, for the prompt reader (<see cref="SkillFileSource.Use"/>).</summary>
    public IReadOnlyList<IFileProvider> Prompts { get; }

    /// <summary>
    /// Every tool name a toolset of this catalogue can register, loaded in an engine or not, case ignored as agent-built
    /// tools are named: an agent-built tool may not take one, or it would collide when a session loads that toolset.
    /// </summary>
    public IReadOnlySet<string> ToolNames { get; }

    /// <summary>Resolves a client's list over this catalogue's toolsets; what <see cref="ToolsetCatalog.Resolve"/> does for core's.</summary>
    public IReadOnlyList<ToolsetDefinition> Resolve(IEnumerable<string>? names, ToolsetContext context) =>
        ToolsetCatalog.Resolve(names, context, Toolsets);

    /// <summary>
    /// The tool names <paramref name="resolved"/> register (an engine-provided toolset contributes none): what a minion's
    /// one tool may be chosen from (F30).
    /// </summary>
    public IReadOnlySet<string> ToolNamesOf(IEnumerable<ToolsetDefinition> resolved) =>
        resolved.SelectMany(NamesOf).ToHashSet(StringComparer.Ordinal);

    /// <summary>The toolset whose class builds <paramref name="tool"/> (a minion's one tool names its toolset, F30); null for none.</summary>
    public string? ToolsetOf(string tool) => _toolsetOfTool.TryGetValue(tool, out var toolset) ? toolset.Name : null;

    /// <summary>
    /// The one rule for which methods are tools: the public instance methods a toolset's class declares. The engine
    /// registers exactly these, and the convertibility test (step 4) checks exactly these.
    /// </summary>
    public static IEnumerable<MethodInfo> ToolMethodsOf(ToolsetDefinition toolset) =>
        toolset.ToolTypes is null ? [] : toolset.ToolTypes.SelectMany(ToolMethodsOfType);

    /// <summary>How a tool method becomes the function the model sees: its <c>[Description]</c> with LF line endings, whatever the checkout.</summary>
    public static AIFunction FunctionOf(MethodInfo method, Func<AIFunctionArguments, object> createInstance) =>
        AIFunctionFactory.Create(method, createInstance, new AIFunctionFactoryOptions
        {
            Description = method.GetCustomAttribute<DescriptionAttribute>() is { } description ? AgentEngine.Normalise(description.Description) : null,
        });

    internal static IEnumerable<MethodInfo> ToolMethodsOfType(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    /// <summary>The one place tool names come from: the tool methods' names today; F36 adds a listed server's names.</summary>
    private static IEnumerable<string> NamesOf(ToolsetDefinition toolset) => ToolMethodsOf(toolset).Select(m => m.Name);

    private static void CheckUnique(IEnumerable<string> names, string kind)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string name in names)
            if (!seen.Add(name))
                throw new AgentCatalogException(MessageText.Format(ErrorMessage.CatalogNameTwice, new { Name = name, Kind = kind }));
    }

    private static void CheckNamed(string owner, IEnumerable<string> named, IReadOnlySet<string> registered)
    {
        foreach (string name in named)
            if (!registered.Contains(name))
                throw new AgentCatalogException(MessageText.Format(ErrorMessage.CatalogPackUnknown,
                    new { Owner = owner, Name = name, Registered = registered.Count == 0 ? "none" : string.Join(", ", registered.Order(StringComparer.Ordinal)) }));
    }

    private static void CheckNoCycle(IReadOnlyList<IAgentPack> packs)
    {
        var byName = packs.ToDictionary(p => p.Name, StringComparer.OrdinalIgnoreCase);
        var done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pack in packs) Visit(pack, []);

        void Visit(IAgentPack pack, List<string> path)
        {
            if (done.Contains(pack.Name)) return;
            int at = path.FindIndex(p => string.Equals(p, pack.Name, StringComparison.OrdinalIgnoreCase));
            if (at >= 0)
                throw new AgentCatalogException(MessageText.Format(ErrorMessage.CatalogRequiresCycle,
                    new { Cycle = string.Join(" -> ", path.Skip(at).Append(pack.Name)) }));
            path.Add(pack.Name);
            foreach (string required in pack.Requires) Visit(byName[required], path);
            path.RemoveAt(path.Count - 1);
            done.Add(pack.Name);
        }
    }
}
