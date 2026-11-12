using System.Globalization;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Messages.Holes.Pages;
using LouisAgent.Api.Messages.Holes.Places;
using LouisAgent.Composition.Messages;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Api.Sessions.Prompts;

/// <summary>
/// The page the visitor is reading as one block of the prompt (F115): where they are, what the page says, cut with a
/// note when it is long, its areas (F117), its links (FX120) and the section on their screen (F118) with the card open
/// there (FX106). A later message from the same page carries only the section on their screen, or nothing.
/// </summary>
internal static class PageBlock
{
    /// <summary>
    /// How much of the page the visitor is reading goes to the model (F115): enough for a site's page, not a book.
    /// </summary>
    internal const int MaxPageChars = 6_000;

    /// <summary>
    /// The page as one block, then the part of the site it is in (F127.7); a page without words is a later message's,
    /// only the section in view.
    /// </summary>
    public static string Of(PageRequest page)
    {
        string block = (page.Text ?? "").Length == 0 ? InViewOnly(page) : Whole(page);
        return block.Length == 0 ? block : block + FocusOf(page);
    }

    /// <summary>The whole page: where the visitor is, what it says, cut with a note when long, its places.</summary>
    private static string Whole(PageRequest page)
    {
        string text = page.Text!;
        string shown = text.Length <= MaxPageChars ? text : text[..MaxPageChars];
        var reading = new PageHoles(page.Url ?? "", page.Title ?? "", shown, AreasOf(page), LinksOf(page),
            InViewOf(page));
        string block = MessageText.Format(ApiModelText.Page, reading);
        if (text.Length <= MaxPageChars) return block;
        var cut = new PageTruncatedHoles(Count(MaxPageChars), Count(text.Length));
        return block + MessageText.Format(ApiModelText.PageTruncated, cut);
    }

    /// <summary>The part of the site the page is in, as a line (F127.7); nothing when the site names none.</summary>
    private static string FocusOf(PageRequest page)
    {
        if (string.IsNullOrWhiteSpace(page.Focus)) return "";
        var part = new PageFocusHoles(page.Focus.Trim());
        return MessageText.Format(ApiModelText.PageFocus, part);
    }

    /// <summary>A later message's page: the section on the visitor's screen and the address, or nothing.</summary>
    private static string InViewOnly(PageRequest page)
    {
        if (page.InView is not { Id: { Length: > 0 } id } inView) return "";
        var onScreen = new PageInViewOnlyHoles(page.Url ?? "", id, inView.Heading ?? "", OpenOf(page));
        return MessageText.Format(ApiModelText.PageInViewOnly, onScreen);
    }

    /// <summary>
    /// The card open on the visitor's screen as a line (FX106), so "this" is the card they opened and not one the guide
    /// opened earlier; nothing when no card is open.
    /// </summary>
    private static string OpenOf(PageRequest page)
    {
        if (page.Open is not { Id: { Length: > 0 } id } open) return "";
        var card = new PageCardOpenHoles(id, open.Heading ?? "");
        return MessageText.Format(ApiModelText.PageCardOpen, card);
    }

    /// <summary>The section on the visitor's screen as a line after the areas (F118), nothing when none is named.</summary>
    private static string InViewOf(PageRequest page)
    {
        if (page.InView is not { Id: { Length: > 0 } id } inView) return "";
        var onScreen = new PageInViewHoles(id, inView.Heading ?? "", page.Url ?? "", OpenOf(page));
        return MessageText.Format(ApiModelText.PageInView, onScreen).TrimEnd();
    }

    /// <summary>
    /// The page's named places as lines of "address heading" after the text (F117; each its own address since F118,
    /// ADR 0107), nothing when the page lists none.
    /// </summary>
    private static string AreasOf(PageRequest page)
    {
        var areas = (page.Areas ?? []).Where(area => !string.IsNullOrWhiteSpace(area.Id)).ToList();
        if (areas.Count == 0) return "";
        var places = new PageAreasHoles(Lines(areas));
        return MessageText.Format(ApiModelText.PageAreas, places);
    }

    /// <summary>
    /// The page's links as lines of "address words" after its areas (FX120), so the guide can open where a link on the
    /// visitor's screen leads; nothing when the page has none.
    /// </summary>
    private static string LinksOf(PageRequest page)
    {
        var links = (page.Links ?? []).Where(link => !string.IsNullOrWhiteSpace(link.Id)).ToList();
        if (links.Count == 0) return "";
        var leads = new PageLinksHoles(Lines(links));
        return MessageText.Format(ApiModelText.PageLinks, leads);
    }

    /// <summary>Each place as one "address words" line.</summary>
    private static string Lines(IEnumerable<PageAreaRequest> places) =>
        string.Join("\n", places.Select(place => $"{place.Id} {place.Heading}".TrimEnd()));

    /// <summary>A count of characters as the model reads it: 6,000.</summary>
    private static string Count(int chars) => chars.ToString("N0", CultureInfo.InvariantCulture);
}
