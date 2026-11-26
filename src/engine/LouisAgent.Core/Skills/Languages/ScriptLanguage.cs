namespace LouisAgent.Core.Skills.Languages;

/// <summary>
/// The language of the script a skill runs (ADR 0043: a name that repeats is an enum), known from the script's file
/// extension (<see cref="ScriptExtension"/>). A member's lower-case name is its wire name.
/// </summary>
public enum ScriptLanguage
{
    /// <summary>A <c>.sh</c> script, run by bash.</summary>
    Bash,

    /// <summary>A <c>.ps1</c> script, run by PowerShell.</summary>
    PowerShell,

    /// <summary>A <c>.py</c> script, run by Python 3.</summary>
    Python,
}
