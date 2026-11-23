using LouisAgent.Api.Site.Places;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Site.Reading;

/// <summary>
/// A page's links within the site, as the guide's widget lists them (FX120): each address once, by its words, so the
/// guide can take the visitor where a link on their screen leads. A link with no words is named by its address.
/// </summary>
internal static class PageLinks
{
    /// <summary>How many characters of a link's words the widget keeps.</summary>
    private const int MostWords = 60;

    /// <summary>How many links the widget sends.</summary>
    private const int MostLinks = 40;

    /// <summary>The links within the site in this markup, in its order.</summary>
    public static List<PageAreaRequest> Of(string markup)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        return
        [
            .. HtmlTag.All(markup)
                .Where(tag => tag.Name == "a" && tag.Attributes.GetValueOrDefault("href") is ['/', ..])
                .Where(tag => seen.Add(tag.Attributes["href"]))
                .Select(tag => LinkOf(tag, markup))
                .Take(MostLinks)
        ];
    }

    /// <summary>A link by its address and its words, or its address when it has none.</summary>
    private static PageAreaRequest LinkOf(HtmlTag tag, string markup)
    {
        string href = tag.Attributes["href"];
        string words = MarkupText.Of(markup[tag.End..tag.ContentEnd(markup)], MostWords);
        return new PageAreaRequest(href, words.Length > 0 ? words : href);
    }
}
