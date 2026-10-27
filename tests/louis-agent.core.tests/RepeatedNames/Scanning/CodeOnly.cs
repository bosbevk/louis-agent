using System.Text;

namespace LouisAgent.Core.Tests.RepeatedNames.Scanning;

/// <summary>
/// A source text with its comments and its string literals blanked out, line for line (F18 step 7.6.1 follow-up): what is left is code, so a ratchet that looks for a type or a call does not take a word
/// in a comment or a string for a use. Every kind of literal is blanked (regular, verbatim, interpolated, raw); the characters stay in place, so a line number still points at the same line.
/// </summary>
public static class CodeOnly
{
    public static string Of(string text)
    {
        var code = new StringBuilder(text.Length);
        int i = 0, n = text.Length;
        while (i < n)
        {
            if (Starts(text, i, "//")) { int end = text.IndexOf('\n', i); int stop = end < 0 ? n : end; Blank(code, text, i, stop); i = stop; }
            else if (Starts(text, i, "/*")) { int end = text.IndexOf("*/", i + 2, StringComparison.Ordinal); int stop = end < 0 ? n : end + 2; Blank(code, text, i, stop); i = stop; }
            else if (RawQuotes(text, i) is { } raw) { int stop = EndOfRaw(text, i, raw); Blank(code, text, i, stop); i = stop; }
            else if (Starts(text, i, "$@\"") || Starts(text, i, "@$\"")) { int stop = EndOfVerbatim(text, i + 3); Blank(code, text, i, stop); i = stop; }
            else if (Starts(text, i, "@\"")) { int stop = EndOfVerbatim(text, i + 2); Blank(code, text, i, stop); i = stop; }
            else if (Starts(text, i, "$\"") || text[i] == '"') { int stop = EndOfRegular(text, i + (text[i] == '$' ? 2 : 1)); Blank(code, text, i, stop); i = stop; }
            else if (text[i] == '\'') { int j = i + 1; while (j < n && text[j] != '\'') j += text[j] == '\\' ? 2 : 1; int stop = Math.Min(j + 1, n); Blank(code, text, i, stop); i = stop; }
            else { code.Append(text[i]); i++; }
        }
        return code.ToString();
    }

    private static bool Starts(string text, int at, string prefix) => string.CompareOrdinal(text, at, prefix, 0, prefix.Length) == 0;

    private static void Blank(StringBuilder code, string text, int from, int to)
    {
        for (int k = from; k < to; k++) code.Append(text[k] == '\n' ? '\n' : ' ');
    }

    private static int? RawQuotes(string text, int at)
    {
        int j = at;
        while (j < text.Length && text[j] == '$') j++;
        int quotes = 0;
        while (j + quotes < text.Length && text[j + quotes] == '"') quotes++;
        return quotes >= 3 ? quotes : null;
    }

    private static int EndOfRaw(string text, int at, int quotes)
    {
        int open = at;
        while (text[open] == '$') open++;
        int end = text.IndexOf(new string('"', quotes), open + quotes, StringComparison.Ordinal);
        return end < 0 ? text.Length : end + quotes;
    }

    private static int EndOfVerbatim(string text, int from)
    {
        for (int j = from; j < text.Length; j++)
            if (text[j] == '"') { if (j + 1 < text.Length && text[j + 1] == '"') { j++; continue; } return j + 1; }
        return text.Length;
    }

    private static int EndOfRegular(string text, int from)
    {
        int j = from;
        while (j < text.Length && text[j] != '"') j += text[j] == '\\' ? 2 : 1;
        return Math.Min(j + 1, text.Length);
    }
}
