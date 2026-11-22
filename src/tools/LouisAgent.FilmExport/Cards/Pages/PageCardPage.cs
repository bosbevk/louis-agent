using System.Globalization;

namespace LouisAgent.FilmExport.Cards.Pages;

/// <summary>
/// A share card's page, ready to photograph (F169.6): a page's card with the site's name and the page's words, or the
/// site's own card with the name and its address. The two templates are files beside this one.
/// </summary>
/// <remarks>
/// The words go in made safe, so a page named with an ampersand shows it as written. Holes are plain text replaced in
/// order, as <see cref="SocialCardPage"/> fills today's card.
/// </remarks>
public static class PageCardPage
{
    /// <summary>A page's card's template, from the repo's root.</summary>
    public const string PageTemplatePath = "src/tools/LouisAgent.FilmExport/Cards/Pages/page-card.html";

    /// <summary>The site's own card's template, from the repo's root.</summary>
    public const string SiteTemplatePath = "src/tools/LouisAgent.FilmExport/Cards/Pages/framework-card.html";

    /// <summary>A page's card: the site's name, the page's name, its first heading and its address.</summary>
    public static string ForPage(string template, CardName name, PageCardWords words) =>
        WithName(template, name).Replace("{page}", Safe(words.Page), StringComparison.Ordinal)
            .Replace("{heading}", Safe(words.Heading), StringComparison.Ordinal)
            .Replace("{address}", Safe(words.Address), StringComparison.Ordinal);

    /// <summary>The site's own card: its name and its address.</summary>
    public static string ForSite(string template, CardName name, string address) =>
        WithName(template, name).Replace("{address}", Safe(address), StringComparison.Ordinal);

    /// <summary>The name's photograph, as a data address, at its size in points.</summary>
    private static string WithName(string template, CardName name) =>
        template.Replace("{name}", $"data:image/png;base64,{Convert.ToBase64String(name.Picture)}",
                StringComparison.Ordinal)
            .Replace("{name-width}", name.Width.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{name-height}", name.Height.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);

    /// <summary>Text made safe inside an element: an ampersand and the angle brackets.</summary>
    private static string Safe(string text) => text.Replace("&", "&amp;", StringComparison.Ordinal)
        .Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);
}
