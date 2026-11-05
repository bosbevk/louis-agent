namespace LouisAgent.Puls.Admin.Verdict;

/// <summary>The verdict's words: passed of total, the change against the run before, the answers to grade.</summary>
/// <remarks>
/// The change is a switch on the verdict's shape, one arm per case, so the five sentences read as a table.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
public static class VerdictText
{
    /// <summary>"3 of 4 passed".</summary>
    public static string Passed(ModeVerdict mode) => $"{mode.Passed} of {mode.Total} passed";

    /// <summary>How this run compares with the run before, in a few words.</summary>
    public static string Change(ModeVerdict mode) => mode switch
    {
        { First: true } => "first run",
        { Change: null } => "the run before has no score",
        { Change: 0 } => "the same as the run before",
        { Change: > 0 } => $"{mode.Change} more than the run before",
        _ => $"{-mode.Change} fewer than the run before"
    };

    /// <summary>The class that colours the change: up, down, the same, or nothing to compare with.</summary>
    public static string Tone(ModeVerdict mode) => mode switch
    {
        { First: true } or { Change: null } => "is-first",
        { Change: 0 } => "is-same",
        { Change: > 0 } => "is-up",
        _ => "is-down"
    };

    /// <summary>"1 answer to grade", "5 answers to grade".</summary>
    public static string ToGrade(int count) => count == 1 ? "1 answer to grade" : $"{count} answers to grade";
}
