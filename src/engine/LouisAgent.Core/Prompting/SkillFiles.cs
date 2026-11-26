using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;

namespace LouisAgent.Core.Prompting;

/// <summary>
/// The skill files core knows by name, by their path in core's project, built from the shared folder names
/// (<see cref="PromptFolder"/>, ADR 0043). Public since CL15, so a pack names its guide the way core does
/// (<see cref="Guide"/>).
/// </summary>
public static class SkillFiles
{
    /// <summary>
    /// The file that marks a demo's <c>skills/</c> as the skills folder when SKILLS_DIRECTORY isn't set: no built-in
    /// file is on disk any more (CL8 step 4), and other folders are called Skills too (core's loader, the tests'
    /// mirror).
    /// </summary>
    internal const string FolderMarker = "README.md";

    /// <summary>
    /// The marker from the folder that holds <c>skills/</c>, which is what a search upward looks for.
    /// </summary>
    internal const string MarkedFolderPath = "skills/" + FolderMarker;

    /// <summary>
    /// What a skill the agent made takes, beside its <c>SKILL.md</c> (F97.1): a JSON Schema, since no standard puts
    /// typed parameters in a <c>SKILL.md</c> and every model's tool takes its inputs as one.
    /// </summary>
    internal const string ParametersFile = "parameters.schema.json";

    /// <summary>The call a skill the agent made was proved with, and its answer (F97.1).</summary>
    internal const string ExampleFile = "examples/example.json";

    /// <summary>
    /// Where a skill the agent made waits as a draft, until it is approved or rejected (F97.1): the folder says its
    /// state, so a draft is never read as approved.
    /// </summary>
    internal const string DraftsFolder = "drafts";

    /// <summary>An approved skill's folder in the skills folder: <c>{name}</c> (F97.1).</summary>
    internal static string ApprovedSkill(string name) => name;

    /// <summary>A draft's folder in the skills folder: <c>drafts/{name}</c> (F97.1).</summary>
    internal static string DraftSkill(string name) => $"{DraftsFolder}/{name}";

    /// <summary>
    /// The principles (ADR 0020): first in every agent's prompt, so nothing later can push them aside.
    /// </summary>
    internal static string Principles { get; } = $"{PromptFolder.Shared.Wire()}/principles.md";

    /// <summary>
    /// The folder of an identity's skills, beside its voice (F129, ADR 0114): <c>agents/{identity}/skills/</c>.
    /// </summary>
    public static string SkillsOf(string identity) =>
        $"{PromptFolder.Agents.Wire()}/{identity}/{PromptFolder.Skills.Wire()}/";

    /// <summary>
    /// A toolset guide's file: <c>toolsets/{name}.md</c>, the same whichever level loads it.
    /// </summary>
    public static string Guide(string name) => $"{PromptFolder.Toolsets.Wire()}/{name}.md";
}
