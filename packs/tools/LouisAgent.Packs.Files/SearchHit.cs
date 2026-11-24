using System.Globalization;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Files.Messages;
using LouisAgent.Packs.Files.Messages.Holes.Search;

namespace LouisAgent.Packs.Files;

/// <summary>
/// A line a search found, as the model is shown it: where it is and what it says. A line no longer than
/// <see cref="MostShown"/> comes back whole; a longer one, such as a recording's one-line JSON or a drawing's SVG,
/// shows the part around the match, marked as cut with its full length, so one hit can't flood the answer.
/// </summary>
/// <remarks>
/// Ask the type (ADR 0123): what a hit shows is decided here, once, and tested on its own, rather than in the search's
/// loop. A hit says where text is; the whole file is ReadWorkspaceFile's to show (FX243: whole giant lines made a
/// search's answer megabytes long, which a second model then had to summarise).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
public static class SearchHit
{
    /// <summary>The most of a line a hit shows, in characters: more than any line of ordinary code.</summary>
    public const int MostShown = 200;

    /// <summary>The hit as <c>path:line: text</c>, the text whole or cut around the first match.</summary>
    public static string Of(string path, int lineNumber, string line, string query)
    {
        string text = line.Trim();
        string where = $"{path}:{lineNumber}: ";
        if (text.Length <= MostShown) return where + text;

        // The window centres on the match, kept inside the line, with a mark at each end that was cut.
        int match = Math.Max(0, text.IndexOf(query, StringComparison.OrdinalIgnoreCase));
        int start = Math.Clamp(match - (MostShown - query.Length) / 2, 0, text.Length - MostShown);
        string before = start > 0 ? "…" : "";
        string after = start + MostShown < text.Length ? "…" : "";
        string length = text.Length.ToString("N0", CultureInfo.InvariantCulture);
        var cut = new HitLineCutHoles(before + text.Substring(start, MostShown) + after, length);
        return where + MessageText.Format(WorkspaceToolMessage.HitLineCut, cut);
    }
}
