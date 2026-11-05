namespace LouisAgent.Composition.Packs;

/// <summary>
/// A toolset a pack offers: its name, what it's for, the settings it still needs, and how to build its tools.
/// </summary>
/// <param name="GuideFile">The guide that loads with the toolset; <see cref="Guide"/> gives the usual path.</param>
/// <param name="ToolTypes">The tool classes, so the catalogue can name their tools without building them.</param>
/// <param name="ParallelSafe">Whether the model's calls to these tools can run at once.</param>
public sealed record PackToolset(
    string Name,
    string Description,
    string? GuideFile,
    Func<IPackContext, IReadOnlyList<string>> MissingSettings,
    Func<IPackContext, IReadOnlyList<object>> CreateTools,
    IReadOnlyList<Type>? ToolTypes = null,
    bool ParallelSafe = false)
{
    /// <summary>Whether everything the toolset needs is set.</summary>
    public bool IsAvailable(IPackContext context) => MissingSettings(context).Count == 0;

    /// <summary>Where a toolset's guide sits among a pack's prompts.</summary>
    public static string Guide(string name) => $"toolsets/{name}.md";
}
