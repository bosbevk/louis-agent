using LouisAgent.Composition.Messages;
using LouisAgent.FilmExport.Messages;
using LouisAgent.FilmExport.Messages.Holes.Cards;

namespace LouisAgent.FilmExport.Cards.Pages;

/// <summary>
/// The words on a page's card: the site's name, the page's name, its first heading and its address. Its description is
/// the same words in one line, written beside the card so the host can say what the picture shows.
/// </summary>
/// <remarks>
/// The description is the card's own words, so a test that reads the page and the description beside its card can
/// tell when a heading has changed and the card has not (Louis's pick, 2026-11-22: a test checks the card's words).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
public sealed record PageCardWords(string Site, string Page, string Heading, string Address)
{
    /// <summary>The card's words in one line, as "louis-agent: Getting started. Your agent, in two lines."</summary>
    public string Description
    {
        get
        {
            var words = new CardDescriptionHoles(Site, Page, Heading);
            return MessageText.Format(FilmExportUserText.CardDescription, words);
        }
    }

    /// <summary>
    /// A page's words from its title, the site's name, its first heading and its address. The page's name is its title
    /// without the site's, as the host's breadcrumb has it; a title that doesn't hold the site's name is refused.
    /// </summary>
    public static PageCardWords Of(string title, string site, string heading, string address)
    {
        string withSite = $" · {site}";
        if (!title.Contains(withSite, StringComparison.Ordinal))
        {
            var refused = new TitleWithoutSiteHoles(title, site);
            throw new InvalidOperationException(MessageText.Format(FilmExportUserText.TitleWithoutSite, refused));
        }

        string page = title.Replace(withSite, "", StringComparison.Ordinal).Trim();
        return new PageCardWords(site, page, heading.Trim(), address);
    }
}
