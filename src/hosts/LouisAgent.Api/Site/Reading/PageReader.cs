using LouisAgent.Api.Site.Places;
using LouisAgent.Api.Site.Rendering;
using LouisAgent.Composition.Sites;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Site.Reading;

/// <summary>
/// Reads a page rendered on the server as the guide's widget reads it in the browser, and gives it as a message
/// carries it: its address, its title, the words of its main part, its places and its links within the site, and
/// the focus the site's rule gives it. What the markup marks hidden is no words and no link. What needs a screen
/// (the section in view, the open card, the selected piece) is never set, and the text is not cut here: the cap is
/// the API's, in one place.
/// </summary>
/// <remarks>
/// Adapter: the places come from the site's own places reader, one walk of the markup, and each is turned into a
/// message's area by its id and its heading, marked by its kind as the widget marks it. Louis's pick over the kind
/// owning its mark in the shared contract and over a second walk of the markup. The answer is the wire's own
/// <see cref="PageRequest" />, so nothing of the wire is typed twice (ADR 0045).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0104-a-message-may-carry-the-page.md">ADR 0104: a message may carry the page</seealso>
internal static class PageReader
{
    /// <summary>The page at this address, its places at their addresses under <paramref name="under" />.</summary>
    public static PageRequest Read(RenderedPage page, string address, string under, string? focus)
    {
        string main = VisibleMarkup.Of(MainOf(page.Body));
        List<PageAreaRequest> areas = [.. PlaceReader.Read(page.Body, under).Select(AreaOf)];
        return new PageRequest(address, page.Title, MarkupText.Of(main), areas, Links: PageLinks.Of(main),
            Focus: focus);
    }

    /// <summary>The markup of the page's main part, or its whole body when it has none, as the widget does.</summary>
    private static string MainOf(string body) =>
        HtmlTag.All(body).FirstOrDefault(tag => tag.Name == "main") is { } main
            ? body[main.End..main.ContentEnd(body)]
            : body;

    /// <summary>A place as a message's area: its id, the last part of its address, and its marked heading.</summary>
    private static PageAreaRequest AreaOf(SitePlace place) =>
        new(place.Address[(place.Address.LastIndexOf('/') + 1)..], HeadingOf(place));

    /// <summary>
    /// The heading the widget gives a place: a card's marked as one that opens, a piece's as one the guide selects,
    /// a section's as it is.
    /// </summary>
    private static string HeadingOf(SitePlace place) => place.Kind switch
    {
        SitePlaceKind.Card => $"{place.Heading} (card, opens)",
        SitePlaceKind.Piece => $"{place.Heading} (piece, selects)",
        _ => place.Heading
    };
}
