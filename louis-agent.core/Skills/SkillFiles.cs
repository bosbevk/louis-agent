namespace LouisAgent.Core.Skills;

/// <summary>The skill files core knows by name.</summary>
internal static class SkillFiles
{
    /// <summary>Skill files loaded by a dedicated provider; discovery skips them to avoid loading them twice, and the
    /// agent may not overwrite them.</summary>
    internal static readonly HashSet<string> BuiltIn = new(StringComparer.OrdinalIgnoreCase)
    {
        "devops-skills.md", "paymo-skills.md", "code-review-skills.md", "time-logging-skills.md",
        "dotnet-skills.md", "python-skills.md", "powershell-skills.md", "bash-skills.md", "web-skills.md", "self-extension-skills.md",
    };
}
