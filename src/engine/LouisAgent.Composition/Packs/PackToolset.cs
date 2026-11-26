using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;

namespace LouisAgent.Composition.Packs;

/// <summary>
/// A toolset a pack offers: its name, what it's for, the settings it still needs, and how to build its tools.
/// </summary>
/// <param name="Skill">
/// The name of the built-in skill the toolset always loads, which tells the model how to use its tools (F97.1);
/// <see cref="SkillFile"/> gives where it sits among the pack's prompts. Null when it loads none.
/// </param>
/// <param name="ToolTypes">The tool classes, so the catalogue can name their tools without building them.</param>
/// <param name="ParallelSafe">Whether the model's calls to these tools can run at once.</param>
public sealed record PackToolset(
    string Name,
    string Description,
    string? Skill,
    Func<IPackContext, IReadOnlyList<string>> MissingSettings,
    Func<IPackContext, IReadOnlyList<object>> CreateTools,
    IReadOnlyList<Type>? ToolTypes = null,
    bool ParallelSafe = false)
{
    /// <summary>Whether everything the toolset needs is set.</summary>
    public bool IsAvailable(IPackContext context) => MissingSettings(context).Count == 0;

    /// <summary>
    /// Where a built-in skill sits among a project's prompts: <c>skills/{name}/SKILL.md</c>, the shape of the Agent
    /// Skills standard.
    /// </summary>
    public static string SkillFile(string name) =>
        $"{PromptFolder.Skills.Wire()}/{name}/{PromptFileName.Skill.Wire()}";
}
