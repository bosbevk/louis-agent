using LouisAgent.Composition.Packs;
using LouisAgent.Core.Agent.Catalog;

namespace LouisAgent.Core.Agent.Toolsets;

/// <summary>
/// One named toolset: what it is, which settings it needs on this instance, the skill it always loads, and how to build
/// its tools.
/// </summary>
/// <param name="Name">The name clients send in <c>toolsets</c>, lowercase.</param>
/// <param name="Description">One line, shown by <c>GET /v1/toolsets</c>.</param>
/// <param name="Skill">
/// The name of the built-in skill that guides the model on these tools, which the toolset always loads (F97.1), at
/// <c>PackToolset.SkillFile</c>'s path; null when it loads none.
/// </param>
/// <param name="MissingSettings">Settings this instance lacks for the toolset; empty when it can be used.</param>
/// <param name="CreateTools">
/// Builds the tool objects for an engine (its public methods become tools). Null when the engine provides the tools
/// itself (<c>skills</c>, see <see cref="AgentEngine"/>).
/// </param>
/// <param name="ParallelSafe">
/// True when the toolset's calls don't depend on each other's order (they read, they change nothing another call
/// reads), so an engine of only such toolsets runs a round's calls at once (F34: the cook's shop searches). False keeps
/// them in order.
/// </param>
/// <param name="ToolTypes">
/// The classes <see cref="CreateTools"/> builds, one object each, so the toolset's tool names are known without
/// building them (<see cref="AgentCatalog.ToolNames"/>); a big toolset is several classes, one concern each (S,
/// CL15), and <c>ToolsetCatalogTests</c> holds that the two lists agree. Null when the engine provides the tools.
/// </param>
public sealed record ToolsetDefinition(
    string Name,
    string Description,
    string? Skill,
    Func<ToolsetContext, IReadOnlyList<string>> MissingSettings,
    Func<ToolsetContext, IReadOnlyList<object>>? CreateTools,
    IReadOnlyList<Type>? ToolTypes = null,
    bool ParallelSafe = false)
{
    /// <summary>
    /// A pack's toolset, as the engine holds it: the pack sees the context only through its contract.
    /// </summary>
    public static ToolsetDefinition Of(PackToolset toolset) =>
        new(toolset.Name, toolset.Description, toolset.Skill, context => toolset.MissingSettings(context),
            context => toolset.CreateTools(context), toolset.ToolTypes, toolset.ParallelSafe);

    /// <summary>True when the engine adds these tools itself rather than from <see cref="CreateTools"/>.</summary>
    public bool IsEngineProvided => CreateTools is null;

    /// <summary>True when this instance has every setting the toolset needs.</summary>
    public bool IsAvailable(ToolsetContext context) => MissingSettings(context).Count == 0;
}
