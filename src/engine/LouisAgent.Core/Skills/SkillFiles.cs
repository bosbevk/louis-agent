using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;

namespace LouisAgent.Core.Skills;

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
    /// The folder of the function skills, the procedures ExecuteSkill runs: level 0's, <c>agents/functions</c>.
    /// </summary>
    internal static string FunctionsFolder { get; } = $"{PromptFolder.Agents.Wire()}/{PromptFolder.Functions.Wire()}";

    /// <summary>
    /// Where a function skill the agent writes waits for the user's <c>/approve</c> (FX20), as agent-built tools wait
    /// in <c>tools/pending/</c>: discovery reads only <see cref="FunctionsFolder"/>'s top, so a later session never
    /// loads it.
    /// </summary>
    internal static string PendingFunctionsFolder { get; } = FunctionsFolder + "/pending";

    /// <summary>
    /// The folder of an identity's skills, beside its voice (F129, ADR 0114): <c>agents/{identity}/skills/</c>.
    /// </summary>
    public static string SkillsOf(string identity) =>
        $"{PromptFolder.Agents.Wire()}/{identity}/{PromptFolder.Skills.Wire()}/";

    /// <summary>A pending function skill's file: <c>agents/functions/pending/{name}.md</c>.</summary>
    internal static string PendingFunction(string name) => $"{PendingFunctionsFolder}/{name}.md";

    /// <summary>
    /// A toolset guide's file: <c>toolsets/{name}.md</c>, the same whichever level loads it.
    /// </summary>
    public static string Guide(string name) => $"{PromptFolder.Toolsets.Wire()}/{name}.md";

    /// <summary>A function skill's file: <c>agents/functions/{name}.md</c>.</summary>
    internal static string Function(string name) => $"{FunctionsFolder}/{name}.md";

    /// <summary>
    /// Whether <paramref name="path"/> is a built-in prompt: what is embedded is built in (CL9, ADR 0019), so the names
    /// the agent may not take follow the files, with no list to keep. Discovery skips one, and <c>CreateSkillFile</c>
    /// refuses it.
    /// </summary>
    internal static bool IsBuiltIn(string path) =>
        SkillFileSource.EmbeddedFiles.Contains(path, StringComparer.OrdinalIgnoreCase);
}
