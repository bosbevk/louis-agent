namespace LouisAgent.Core.Skills;

/// <summary>The skill files core knows by name, relative to the skills folder (CL8: in the folder of the level that reads them).</summary>
internal static class SkillFiles
{
    /// <summary>The folder of the toolset guides, which every level shares (a guide is the same whichever level loads it).</summary>
    internal const string ToolsetsFolder = "toolsets";

    /// <summary>The router: it picks a toolset and delegates, so only level 0 has it.</summary>
    internal const string Router = Personalities.AgentFolder + "/default.md";

    /// <summary>A toolset guide's file: <c>toolsets/{name}.md</c>.</summary>
    internal static string Guide(string name) => $"{ToolsetsFolder}/{name}.md";

    /// <summary>Skill files <c>AgentHost</c> loads by name (the toolset guides and the function files); discovery skips
    /// them to avoid loading them twice, and the agent may not overwrite them.</summary>
    internal static readonly HashSet<string> BuiltIn = new(StringComparer.OrdinalIgnoreCase)
    {
        Guide("devops"), Guide("paymo"), Guide("files"), Guide("git"), Guide("agents"), "code-review-skills.md", "time-logging-skills.md",
        Guide("dotnet"), Guide("python"), Guide("powershell"), Guide("bash"), Guide("web"), Guide("self-extension"),
    };
}
