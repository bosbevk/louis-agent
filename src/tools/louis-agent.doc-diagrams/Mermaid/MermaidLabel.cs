using System.Text;

namespace LouisAgent.DocDiagrams.Mermaid;

/// <summary>
/// A Mermaid label as plain words: a <c>&lt;br/&gt;</c> a new line, any other tag left out, an entity its character,
/// each line trimmed.
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
            .Replace("<br>", "\n", StringComparison.OrdinalIgnoreCase);
        text = WithoutTags(text);
        foreach (var (entity, character) in Entities) text = text.Replace(entity, character, StringComparison.Ordinal);
        return string.Join('\n', text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0));
    }

    /// <summary>The text with every tag (<c>&lt;b&gt;</c>, <c>&lt;/i&gt;</c>) taken out and its words kept.</summary>
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
