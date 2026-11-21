using System.ComponentModel;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Sites;
using LouisAgent.Packs.Site.Messages;
using LouisAgent.Packs.Site.Messages.Holes.Pages;

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
public sealed class SiteTools(ISitePlaces? places = null)
{
    private readonly ISitePlaces _places = places ?? NoSitePlaces.Instance;

    private const int MostPlaces = 5;

    /// <summary>
    /// Finds places on any page of the site by the visitor's words (F127.7), so the guide can show one next: the
    /// site's own pages are the index, never a list in the prompt.
    /// </summary>
    [Description(
        "Finds places on any page of this site by a few words: its sections, cards and pieces, each with its own " +
        "address, the best first. Use it when what they ask about may be on another page, then show the right one " +
        "with ShowPage.")]
    public async Task<string> FindPlace(
        [Description("A few words for what they ask about, such as prompt caching or the guesthouse.")]
        string words,
        CancellationToken cancellationToken = default)
    {
        if (!_places.HasSite) return MessageText.Format(SiteToolMessage.NoSiteToSearch);

        var found = await _places.FindAsync(words, MostPlaces, cancellationToken);
        if (found.Count == 0)
        {
            var asked = new NoPlaceFoundHoles(words);
            return MessageText.Format(SiteToolMessage.NoPlaceFound, asked);
        }

        // One line a place, then the list under the words searched for.
        var lines = found.Select(place => MessageText.Format(SiteToolMessage.PlaceLine, PlaceLineHoles.Of(place)));
        var list = new PlacesFoundHoles(words, string.Join('\n', lines));
        return MessageText.Format(SiteToolMessage.PlacesFound, list);
    }

    /// <summary>Takes the visitor to a page of this site, or to an area of it.</summary>
    [Description(
        "Opens a place of this site in the visitor's browser by its own address, and lights it up: a page, a section " +
        "of one, a card, which opens, or a page of a section paged sideways, which turns to it. Use it when the " +
        "right place exists for what they ask; then say in one line what to look at. The page you are on lists its " +
        "places by their addresses.")]
    public string ShowPage(
        [Description(
            "The place's address as the page lists it, such as /framework/packs, /framework/identities/levels, " +
            "/framework/identities/puls, /home/rule or /framework/how-it-works/loop/2.")]
        string path)
    {
        var place = new PlaceHoles(path);
        if (!IsSiteAddress(path)) return MessageText.Format(SiteToolMessage.NotASitePage, place);
        return MessageText.Format(SiteToolMessage.Shown, place);
    }

    /// <summary>
    /// An address of this site (ADR 0107): one slash first, then words of lowercase letters, digits and dashes, one
    /// after another, the last perhaps the number of a paged section's page from one up. Nothing else: no scheme, no
    /// host, no climb out, no space, no fragment, no query.
    /// </summary>
    private static bool IsSiteAddress(string path)
    {
        if (path == "/") return true;
        if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/') || path.EndsWith('/')) return false;

        string[] words = path[1..].Split('/');
        string last = words[^1];
        bool turned = last.All(char.IsAsciiDigit);
        if (turned && !IsPage(last)) return false;
        return (turned ? words[..^1] : words).All(IsWord) && (!turned || words.Length > 1);
    }

    /// <summary>A word of an address: a letter or digit first, then letters, digits and dashes.</summary>
    private static bool IsWord(string word) =>
        word.Length > 0 && (char.IsAsciiLetterLower(word[0]) || char.IsAsciiDigit(word[0])) &&
        word.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-');

    /// <summary>A page of a paged section: a number from one up, written plainly.</summary>
    private static bool IsPage(string page) => int.TryParse(page, out int number) && number >= 1 && page[0] != '0';
}
