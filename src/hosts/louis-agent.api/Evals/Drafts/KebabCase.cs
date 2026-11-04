namespace LouisAgent.Api.Evals.Drafts;

/// <summary>
/// Says whether a name a person typed can become part of a file's name: lowercase letters and digits joined by single
/// dashes, as the scenario ids and the known-bad reasons in the eval folders are (<c>cook-af</c>, <c>no-route</c>).
/// </summary>
/// <remarks>
/// A small reader instead of a regular expression (ADR 0057): one pass over the characters, each part between dashes
/// non-empty, so nothing like <c>../</c>, a space or a capital gets into a path.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0057-regex-is-the-last-option.md">ADR 0057: regex is the last option</seealso>
public static class KebabCase
{
    /// <summary>The longest name we take, so one request can't make a file name the disk refuses.</summary>
    public const int MaxLength = 80;

    /// <summary>Whether the name is kebab-case, and short enough to be part of a file's name.</summary>
    public static bool Is(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > MaxLength) return false;

        // Every part between the dashes holds at least one lowercase letter or digit, and nothing else.
        return name.Split('-')
            .All(part => part.Length > 0 && part.All(c => c is >= 'a' and <= 'z' or >= '0' and <= '9'));
    }
}
