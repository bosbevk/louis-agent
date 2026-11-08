using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Site.Messages;

namespace LouisAgent.Packs.Site.Tools;

/// <summary>
/// The <c>site</c> toolset (F117): one tool that takes the visitor to a page of the site, and to an area of it. The
/// tool itself moves nothing: the web's widget hears the call in the turn's stream and moves the browser; here the
/// path is checked, so the model only ever names a page of this site.
/// </summary>
/// <remarks>
/// A tool for a side effect the client performs (ADR 0080): the model asks for the page as data, the API answers that
/// it is shown, and the widget acts on the call. The check is the whole job of this class, so a path that leaves the
/// site (another host, a scheme, a climb out) is refused with the reason and never reaches a browser.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0004-arguments-as-data.md">ADR 0004: arguments as data</seealso>
public sealed class SiteTools
{
    /// <summary>Takes the visitor to a page of this site, or to an area of it.</summary>
    [Description(
        "Opens a page of this site in the visitor's browser, scrolled to one of its areas when the path names one " +
        "after #, and lights the area up. A card is an area too, and it opens; a section paged sideways turns to the " +
        "page named after a slash. Use it when the right page, area or card exists for what they ask; then say in " +
        "one line what to look at. The page you are on lists its areas, and a paged section's pages.")]
    public string ShowPage(
        [Description("The page's path on this site, such as /packs, with an area or a card after # for a part of it, such as /identities#levels or /identities#puls, and a page of a paged section after a slash, such as /how-it-works#parts/2.")]
        string path)
    {
        if (!IsSitePath(path)) return MessageText.Format(SiteToolMessage.NotASitePage, new { Path = path });
        return MessageText.Format(SiteToolMessage.Shown, new { Path = path });
    }

    /// <summary>
    /// A path of this site: it starts with one slash, carries no scheme, no host, no climb out and no space, and an
    /// area after the hash, when there is one, is an id in words: a letter first, then letters, digits and dashes,
    /// as a card's id made from its title can hold a number (F118). A section paged sideways may name its page after
    /// a slash, from one up: <c>#parts/2</c>.
    /// </summary>
    private static bool IsSitePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/') || path.StartsWith("//")) return false;
        if (path.Contains(':') || path.Contains("..") || path.Any(char.IsWhiteSpace)) return false;

        int hash = path.IndexOf('#');
        if (hash < 0) return true;
        string[] parts = path[(hash + 1)..].Split('/');
        if (parts.Length > 2) return false;
        if (parts.Length == 2 && !IsPage(parts[1])) return false;
        return IsAreaId(parts[0]);
    }

    /// <summary>An area's id in words: a letter first, then letters, digits and dashes.</summary>
    private static bool IsAreaId(string area) =>
        area.Length > 0 && char.IsAsciiLetterLower(area[0]) &&
        area.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');

    /// <summary>A page of a paged section: a number from one up.</summary>
    private static bool IsPage(string page) => int.TryParse(page, out int number) && number >= 1 && page[0] != '0';
}
