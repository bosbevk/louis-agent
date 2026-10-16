namespace LouisAgent.Core.Skills;

/// <summary>The skill files core knows by name, by their path under core's Prompts/ (CL8: in the folder of the level that reads them).</summary>
internal static class SkillFiles
{
    /// <summary>
    /// The file that marks the repo's <c>Skills/</c> as the skills folder when SKILLS_DIRECTORY isn't set: no built-in file
    /// is on disk any more (CL8 step 4), and other folders are called Skills too (core's loader, the tests' mirror).
    /// </summary>
    internal const string FolderMarker = "README.md";

    /// <summary>The folder of the toolset guides, which every level shares (a guide is the same whichever level loads it).</summary>
    internal const string ToolsetsFolder = "toolsets";

    /// <summary>The folder of the function skills: procedures ExecuteSkill runs, chosen by AGENT_FUNCTION (level 0's).</summary>
    internal const string FunctionsFolder = Personalities.AgentFolder + "/functions";

    /// <summary>A toolset guide's file: <c>toolsets/{name}.md</c>.</summary>
    internal static string Guide(string name) => $"{ToolsetsFolder}/{name}.md";

    /// <summary>A function skill's file: <c>agent/functions/{name}.md</c>.</summary>
    internal static string Function(string name) => $"{FunctionsFolder}/{name}.md";

    /// <summary>Skill files <c>AgentHost</c> loads by name (the toolset guides and the function files); discovery skips
    /// them to avoid loading them twice, and the agent may not overwrite them.</summary>
    internal static readonly HashSet<string> BuiltIn = new(StringComparer.OrdinalIgnoreCase)
    {
        Guide("devops"), Guide("paymo"), Guide("files"), Guide("git"), Guide("agents"), Function("code-review"),
        Guide("dotnet"), Guide("python"), Guide("powershell"), Guide("bash"), Guide("web"), Guide("self-extension"),
    };
}
