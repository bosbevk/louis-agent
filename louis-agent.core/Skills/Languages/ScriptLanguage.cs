namespace LouisAgent.Core.Skills.Languages;

/// <summary>
/// The language of a script a skill or an agent-built tool runs (ADR 0043: a name that repeats is an enum): the tag on a code fence, parsed once where the file
/// is read (<c>MarkdownSkillLoader.NormalizeLanguage</c>, with its aliases <c>pwsh</c>, <c>py</c>, <c>sh</c>). A member's lower-case name is its wire name.
/// </summary>
public enum ScriptLanguage
{
    Bash,
    PowerShell,
    Python,
}
