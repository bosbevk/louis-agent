using System.Net;
using System.Text;

namespace LouisAgent.Api.Site.Places;

/// <summary>
/// The words a visitor reads in some markup. The site's places and a page read for a message both ask it, so a
/// place's words and a page's text are read by one rule (ADR 0123).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
internal static class MarkupText
{
    /// <summary>
    /// The words of some markup: tags gone, what a style or a script holds gone with them (F164.2), entities decoded,
    /// white space folded, cut at the most when a most is given.
    /// </summary>
    public static string Of(string markup, int most = int.MaxValue)
    {
        // Every tag, opening or closing, becomes a space, so the words of two elements never run together.
        var text = new StringBuilder();
        int at = 0;
        for (int open = markup.IndexOf('<'); open >= 0; open = markup.IndexOf('<', at))
        {
            int close = markup.IndexOf('>', open);
            if (close < 0) break;
            text.Append(markup[at..open]).Append(' ');
            at = PastHiddenContent(markup, open, close) + 1;
        }

        string words = WebUtility.HtmlDecode(text.Append(markup[at..]).ToString());
        string folded = string.Join(' ', words.Split((char[])[' ', '\n', '\r', '\t'],
            StringSplitOptions.RemoveEmptyEntries));
        return folded.Length <= most ? folded : folded[..most].TrimEnd();
    }

    /// <summary>The elements whose content a visitor never reads as words: a diagram's rules, a page's code.</summary>
    private static readonly string[] Hidden = ["style", "script"];

    /// <summary>
    /// Where a tag ends: its own <c>&gt;</c>, or, for an opening style or script, the end of its closing tag, so
    /// what it holds is skipped with it.
    /// </summary>
    private static int PastHiddenContent(string markup, int open, int close)
    {
        string? hidden = Hidden.FirstOrDefault(name => OpensElement(markup, open, name));
        if (hidden is null) return close;

        int end = markup.IndexOf($"</{hidden}", close, StringComparison.OrdinalIgnoreCase);
        int endClose = end < 0 ? -1 : markup.IndexOf('>', end);
        return endClose < 0 ? markup.Length - 1 : endClose;
    }

    /// <summary>Whether the tag at <paramref name="open"/> opens an element of this name, case ignored.</summary>
    private static bool OpensElement(string markup, int open, string name)
    {
        int after = open + 1 + name.Length;
        return after < markup.Length &&
               string.Compare(markup, open + 1, name, 0, name.Length, StringComparison.OrdinalIgnoreCase) == 0 &&
               markup[after] is ' ' or '>' or '/' or '\t' or '\n' or '\r';
    }
}
