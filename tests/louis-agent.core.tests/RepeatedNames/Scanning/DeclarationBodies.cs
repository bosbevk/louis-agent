using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests.RepeatedNames.Scanning;

/// <summary>
/// Where the bodies of declarations are in a source text (F18 step 7.5.4f): every <c>enum</c>'s braces, and the braces of the classes asked for by name. The ratchet skips a name written
/// inside one of them, because that is where a name is written once; every other line of the file, an enum file included, is scanned like any other.
/// </summary>
public static class DeclarationBodies
{
    private static readonly Regex EnumHeader = new(@"\benum\s+\w+[^{;]*\{", RegexOptions.Compiled);

    public static IReadOnlyList<(int Start, int End)> Spans(string text, IReadOnlyCollection<string>? classNames = null)
    {
        var spans = new List<(int, int)>();
        foreach (Match header in EnumHeader.Matches(text)) spans.Add((header.Index, EndOfBlock(text, header.Index + header.Length - 1)));
        foreach (string name in classNames ?? [])
            foreach (Match header in Regex.Matches(text, $@"\bclass\s+{Regex.Escape(name)}\b[^{{;]*\{{"))
                spans.Add((header.Index, EndOfBlock(text, header.Index + header.Length - 1)));
        return spans;
    }

    public static bool Contains(IReadOnlyList<(int Start, int End)> spans, int index) => spans.Any(s => index >= s.Start && index <= s.End);

    /// <summary>The index of the brace that closes the one at <paramref name="open"/>; a brace inside a string or a comment does not count.</summary>
    private static int EndOfBlock(string text, int open)
    {
        int depth = 0;
        for (int i = open; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '"') { i++; while (i < text.Length && text[i] != '"') i += text[i] == '\\' ? 2 : 1; }
            else if (c == '/' && i + 1 < text.Length && text[i + 1] == '/') { int end = text.IndexOf('\n', i); i = end < 0 ? text.Length : end; }
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return i;
        }
        return text.Length;
    }
}
