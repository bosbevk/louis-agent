namespace LouisAgent.Core.Skills;

/// <summary>The skill files core knows by name, by their path under core's Prompts/ (CL8: in the folder of the level that reads them).</summary>
internal static class SkillFiles
{
    /// <summary>
    /// The file that marks the repo's <c>Skills/</c> as the skills folder when SKILLS_DIRECTORY isn't set: no built-in file
    /// is on disk any more (CL8 step 4), and other folders are called Skills too (core's loader, the tests' mirror).
    /// </summary>
    internal const string FolderMarker = "README.md";

    /// <summary>The folder of what every agent loads, at every level (CL9): the principles.</summary>
    internal const string SharedFolder = "shared";

    /// <summary>The principles (ADR 0020): first in every agent's prompt, so nothing later can push them aside.</summary>
    internal const string Principles = SharedFolder + "/principles.md";

    /// <summary>The folder of the toolset guides, which every level shares (a guide is the same whichever level loads it).</summary>
    internal const string ToolsetsFolder = "toolsets";

    /// <summary>
    /// The folder of the function skills, the procedures ExecuteSkill runs: level 0's, under <c>agent/</c>. Written out,
    /// not built from <c>RoleCatalog.AgentFolder</c>, so the top of <c>Skills/</c> uses nothing below it (CL13);
    /// <c>SkillFilesTests</c> holds the two together.
    /// </summary>
    internal const string FunctionsFolder = "agent/functions";

    /// <summary>
    /// Where a function skill the agent writes waits for the user's <c>/approve</c> (FX20), as agent-built tools wait in
    /// <c>tools/pending/</c>: discovery reads only <see cref="FunctionsFolder"/>'s top, so a later session never loads it.
    /// </summary>
    internal const string PendingFunctionsFolder = FunctionsFolder + "/pending";

    /// <summary>A pending function skill's file: <c>agent/functions/pending/{name}.md</c>.</summary>
    internal static string PendingFunction(string name) => $"{PendingFunctionsFolder}/{name}.md";

    /// <summary>A toolset guide's file: <c>toolsets/{name}.md</c>.</summary>
    internal static string Guide(string name) => $"{ToolsetsFolder}/{name}.md";

    /// <summary>A function skill's file: <c>agent/functions/{name}.md</c>.</summary>
    internal static string Function(string name) => $"{FunctionsFolder}/{name}.md";

    /// <summary>
    /// Whether <paramref name="path"/> is a built-in prompt: what is embedded is built in (CL9, ADR 0019), so the names the
    /// agent may not take follow the files, with no list to keep. Discovery skips one, and <c>CreateSkillFile</c> refuses it.
    /// </summary>
    internal static bool IsBuiltIn(string path) => SkillFileSource.EmbeddedFiles.Contains(path, StringComparer.OrdinalIgnoreCase);
}
