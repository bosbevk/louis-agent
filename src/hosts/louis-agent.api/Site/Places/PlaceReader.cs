using System.Net;
using System.Text;
using LouisAgent.Composition.Sites;

namespace LouisAgent.Api.Site.Places;

/// <summary>
/// Reads the places from a rendered page (F127.7), by the same marks the guide's widget reads in the browser: a
/// section with an id, a card (<c>data-card</c>) and a piece (<c>data-piece</c>), each with the heading the widget
/// would give it and its own words.
/// </summary>
internal static class PlaceReader
{
    /// <summary>How many characters of a place's words the index keeps.</summary>
    private const int MostText = 300;

    /// <summary>The page's places in its order, each at its address under <paramref name="under"/>.</summary>
    public static IEnumerable<SitePlace> Read(string html, string under)
    {
        foreach (var tag in HtmlTag.All(html))
        {
            if (tag.Id is not { Length: > 0 } id || KindOf(tag) is not { } kind) continue;
            string inner = html[tag.End..tag.ContentEnd(html)];
            yield return new SitePlace($"{under.TrimEnd('/')}/{id}", HeadingOf(inner, kind) ?? id, kind,
                TextOf(inner, MostText));
        }
    }

    /// <summary>What the tag is, or null when it is no place.</summary>
    /// <remarks>Guard clauses, one kind each (ADR 0051): a card is a piece too, so it is asked first.</remarks>
    private static SitePlaceKind? KindOf(HtmlTag tag)
    {
        if (tag.Has("data-card")) return SitePlaceKind.Card;
        if (tag.Has("data-piece")) return SitePlaceKind.Piece;
        if (tag.Name == "section") return SitePlaceKind.Section;
        return null;
    }

    /// <summary>
    /// The heading, as the widget picks it: the first of a card's title; a piece's heading or number; a section's
    /// kicker or heading.
    /// </summary>
    private static string? HeadingOf(string inner, SitePlaceKind kind)
    {
        var heading = HtmlTag.All(inner).FirstOrDefault(tag => kind switch
        {
            SitePlaceKind.Card => tag.HasClass("card-title"),
            SitePlaceKind.Piece => tag.Name is "h3" or "h4" || tag.HasClass("stat-value"),
            _ => tag.HasClass("kicker") || tag.Name is "h1" or "h2" or "h3",
        });
        return heading is null ? null : TextOf(inner[heading.End..heading.ContentEnd(inner)], MostText);
    }

    /// <summary>The words of some markup: tags gone, entities decoded, white space folded, cut at the most.</summary>
    private static string TextOf(string markup, int most)
    {
        // Every tag, opening or closing, becomes a space, so the words of two elements never run together.
        var text = new StringBuilder();
        int at = 0;
        for (int open = markup.IndexOf('<'); open >= 0; open = markup.IndexOf('<', at))
        {
            int close = markup.IndexOf('>', open);
            if (close < 0) break;
            text.Append(markup[at..open]).Append(' ');
            at = close + 1;
        }

        string words = WebUtility.HtmlDecode(text.Append(markup[at..]).ToString());
        string folded = string.Join(' ', words.Split((char[])[' ', '\n', '\r', '\t'],
            StringSplitOptions.RemoveEmptyEntries));
        return folded.Length <= most ? folded : folded[..most].TrimEnd();
    }
}
