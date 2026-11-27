using System.Text;

namespace LouisAgent.Core.Agent.EngineTools.ToolSearch;

/// <summary>
/// The words a tool search compares (F97.3): a text's runs of letters and digits in lower case, a tool's name split
/// where a capital starts a word, and the small words that tell a search nothing left out.
/// </summary>
/// <remarks>Plain loops over the characters, no regular expression (ADR 0057).</remarks>
internal static class ToolWords
{
    /// <summary>The shortest word that can match as the start of a longer one: "file" finds "files".</summary>
    private const int ShortestPrefix = 4;

    /// <summary>Words that say nothing about which tool is wanted.</summary>
    private static readonly HashSet<string> Small =
    [
        "a", "an", "the", "me", "my", "i", "you", "your", "what", "which", "this", "that", "these", "those", "in", "of",
        "to", "for", "and", "or", "is", "are", "it", "its", "on", "with", "by", "from", "how", "do", "does", "all",
        "any", "one", "some", "please", "can", "could", "would"
    ];

    /// <summary>
    /// A text's words, in lower case, the small ones left out: "List the .NET projects" is list, net, projects.
    /// </summary>
    public static IReadOnlyList<string> Of(string text)
    {
        var words = new List<string>();
        var word = new StringBuilder();
        foreach (char c in text + " ")
        {
            if (char.IsLetterOrDigit(c))
            {
                word.Append(char.ToLowerInvariant(c));
                continue;
            }

            // A word ends at anything that isn't a letter or a digit.
            if (word.Length > 0 && !Small.Contains(word.ToString())) words.Add(word.ToString());
            word.Clear();
        }

        return words;
    }

    /// <summary>
    /// A tool's name as words, split where a capital starts one: DotNetListProjects is dot, net, list, projects.
    /// </summary>
    public static IReadOnlyList<string> OfName(string name)
    {
        var spaced = new StringBuilder();
        foreach (char c in name)
        {
            if (char.IsUpper(c) && spaced.Length > 0) spaced.Append(' ');
            spaced.Append(c);
        }

        return Of(spaced.ToString());
    }

    /// <summary>
    /// Whether two words are the same, or one starts the other and is long enough to mean it: "commit" and "commits".
    /// </summary>
    public static bool Match(string a, string b) =>
        a == b || (Math.Min(a.Length, b.Length) >= ShortestPrefix && (a.StartsWith(b) || b.StartsWith(a)));
}
