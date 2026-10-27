namespace LouisAgent.Core.Tests.RepeatedNames.Scanning;

/// <summary>
/// A small C# lexer for the repeated-names ratchet (F18 step 7.5.3): the standalone regular string literals of a source text, each with its line.
/// A name inside a raw, verbatim or interpolated string is wire data (an expected JSON, a message) and stays text, so those are skipped; so are comments and chars.
/// </summary>
public static class StringLiterals
{
    public sealed record Literal(string Value, int Line, int Column, int Index);

    public static IReadOnlyList<Literal> Regular(string text)
    {
        var found = new List<Literal>();
        int i = 0, line = 1, n = text.Length;
        while (i < n)
        {
            char c = text[i];
            if (c == '\n') { line++; i++; }
            else if (Starts(text, i, "//")) i = text.IndexOf('\n', i) is var e && e < 0 ? n : e;
            else if (Starts(text, i, "/*")) { int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal); int stop = end < 0 ? n : end + 2; line += Lines(text, i, stop); i = stop; }
            else if (RawStart(text, i) is { } raw) { int stop = SkipRaw(text, i, raw); line += Lines(text, i, stop); i = stop; }
            else if (Starts(text, i, "$@\"") || Starts(text, i, "@$\"")) { int stop = SkipVerbatim(text, i + 3); line += Lines(text, i, stop); i = stop; }
            else if (Starts(text, i, "@\"")) { int stop = SkipVerbatim(text, i + 2); line += Lines(text, i, stop); i = stop; }
            else if (Starts(text, i, "$\"")) { int stop = SkipInterpolated(text, i + 2); line += Lines(text, i, stop); i = stop; }
            else if (c == '"')
            {
                int j = i + 1;
                while (j < n && text[j] != '"') j += text[j] == '\\' ? 2 : 1;
                found.Add(new Literal(text[(i + 1)..Math.Min(j, n)], line, i - text.LastIndexOf('\n', i) - 1, i));
                i = j + 1;
            }
            else if (c == '\'') { int j = i + 1; while (j < n && text[j] != '\'') j += text[j] == '\\' ? 2 : 1; i = j + 1; }
            else i++;
        }
        return found;
    }

    private static bool Starts(string text, int at, string prefix) => string.CompareOrdinal(text, at, prefix, 0, prefix.Length) == 0;

    private static int Lines(string text, int from, int to) => text.AsSpan(from, Math.Min(to, text.Length) - from).Count('\n');

    /// <summary>The quote count of a raw string literal starting at <paramref name="at"/> (three or more, after any <c>$</c>), or null.</summary>
    private static int? RawStart(string text, int at)
    {
        int j = at;
        while (j < text.Length && text[j] == '$') j++;
        int quotes = 0;
        while (j + quotes < text.Length && text[j + quotes] == '"') quotes++;
        return quotes >= 3 ? quotes : null;
    }

    private static int SkipRaw(string text, int at, int quotes)
    {
        int open = at;
        while (text[open] == '$') open++;
        int end = text.IndexOf(new string('"', quotes), open + quotes, StringComparison.Ordinal);
        return end < 0 ? text.Length : end + quotes;
    }

    private static int SkipVerbatim(string text, int from)
    {
        int j = from;
        while (j < text.Length)
        {
            if (text[j] == '"') { if (j + 1 < text.Length && text[j + 1] == '"') { j += 2; continue; } return j + 1; }
            j++;
        }
        return text.Length;
    }

    private static int SkipInterpolated(string text, int from)
    {
        int j = from, depth = 0;
        while (j < text.Length)
        {
            char ch = text[j];
            if (ch == '\\') { j += 2; continue; }
            if (ch == '{') { if (Starts(text, j, "{{")) { j += 2; continue; } depth++; }
            else if (ch == '}') { if (depth == 0 && Starts(text, j, "}}")) { j += 2; continue; } depth = Math.Max(0, depth - 1); }
            else if (ch == '"' && depth == 0) return j + 1;
            j++;
        }
        return text.Length;
    }
}
