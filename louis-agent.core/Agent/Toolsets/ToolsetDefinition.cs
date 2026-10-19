namespace LouisAgent.Core.Agent.Toolsets;

using LouisAgent.Core.Agent.Catalog;

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
/// <param name="ParallelSafe">
/// True when the toolset's calls don't depend on each other's order (they read, they change nothing another call reads),
/// so an engine of only such toolsets runs a round's calls at once (F34: the cook's shop searches). False keeps them in
/// order.
/// </param>
/// <param name="ToolType">
/// The class <see cref="CreateTools"/> builds, so its tool names are known without building it (<see cref="AgentCatalog.ToolNames"/>).
/// Null when the engine provides the tools.
/// </param>
public sealed record ToolsetDefinition(
    string Name,
    string Description,
    string? GuideFile,
    Func<ToolsetContext, IReadOnlyList<string>> MissingSettings,
    Func<ToolsetContext, IReadOnlyList<object>>? CreateTools,
    Type? ToolType = null,
    bool ParallelSafe = false)
{
    /// <summary>True when the engine adds these tools itself rather than from <see cref="CreateTools"/>.</summary>
    public bool IsEngineProvided => CreateTools is null;

    /// <summary>True when this instance has every setting the toolset needs.</summary>
    public bool IsAvailable(ToolsetContext context) => MissingSettings(context).Count == 0;
}
