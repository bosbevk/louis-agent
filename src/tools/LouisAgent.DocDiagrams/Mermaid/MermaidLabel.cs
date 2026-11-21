using System.Text;

namespace LouisAgent.DocDiagrams.Mermaid;

/// <summary>
/// A Mermaid label as plain words: a <c>&lt;br/&gt;</c> or a written <c>\n</c> a new line, any other tag left out, an
/// entity its character, each line trimmed.
/// </summary>
internal static class MermaidLabel
{
    /// <summary>The entities the docs' labels use, and what each stands for.</summary>
    private static readonly (string Entity, string Character)[] Entities =
    [
        ("&quot;", "\""), ("#quot;", "\""), ("&amp;", "&"), ("&lt;", "<"), ("#lt;", "<"), ("&gt;", ">"), ("#gt;", ">"),
        ("&#39;", "'")
    ];

    /// <summary>The label's words, its breaks as new lines.</summary>
    public static string Clean(string raw)
    {
        string text = raw.Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("\\n", "\n", StringComparison.Ordinal);
        text = WithoutTags(text);
        foreach (var (entity, character) in Entities) text = text.Replace(entity, character, StringComparison.Ordinal);
        return string.Join('\n', text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0));
    }

    /// <summary>The text with every tag (<c>&lt;b&gt;</c>, <c>&lt;/i&gt;</c>) taken out and its words kept.</summary>
    /// <summary>
    /// A sequence's words as they read (CL60.3): a break as a new line and an entity or a <c>#59;</c> code as its
    /// character, but every <c>&lt;</c> kept, since Mermaid reads no tags in a message and a generic is code.
    /// </summary>
    public static string Text(string raw)
    {
        string text = raw.Replace("<br/>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br />", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase)
            .Replace("\\n", "\n", StringComparison.Ordinal);
        foreach (var (entity, character) in Entities) text = text.Replace(entity, character, StringComparison.Ordinal);
        text = Codes(text);
        return string.Join('\n', text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0));
    }

    /// <summary>Each <c>#59;</c>-style code, a character's number, as the character Mermaid shows for it.</summary>
    private static string Codes(string text)
    {
        var kept = new StringBuilder(text.Length);
        for (int at = 0; at < text.Length; at++)
        {
            int end = text[at] == '#' ? text.IndexOf(';', at) : -1;
            bool code = end > at + 1 && text[(at + 1)..end].All(char.IsAsciiDigit);
            if (!code)
            {
                kept.Append(text[at]);
                continue;
            }

            kept.Append((char)int.Parse(text[(at + 1)..end], System.Globalization.CultureInfo.InvariantCulture));
            at = end;
        }

        return kept.ToString();
    }

    private static string WithoutTags(string text)
    {
        var kept = new StringBuilder(text.Length);
        for (int at = 0; at < text.Length; at++)
        {
            // A tag starts with < and a letter or a slash, and runs to its >.
            bool tag = text[at] == '<' && at + 1 < text.Length && (char.IsLetter(text[at + 1]) || text[at + 1] == '/');
            int close = tag ? text.IndexOf('>', at) : -1;
            if (close < 0)
            {
                kept.Append(text[at]);
                continue;
            }

            at = close;
        }

        return kept.ToString();
    }
}
