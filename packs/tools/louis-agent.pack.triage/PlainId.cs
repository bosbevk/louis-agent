namespace LouisAgent.Packs.Triage;

/// <summary>
/// An id this pack accepts from outside: letters, digits, dashes and underscores, up to 64 of them. Ids end up in
/// branch names, commit messages and file names, so anything else is refused where it enters.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
internal static class PlainId
{
    /// <summary>Whether the id can travel as it is.</summary>
    public static bool Is(string? id) =>
        id is { Length: > 0 and <= 64 } && id.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');
}
