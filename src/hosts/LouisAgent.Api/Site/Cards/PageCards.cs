using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Api.Site.Cards;

/// <summary>
/// Finds a page's own share card in the site's web root: <c>/getting-started</c> wears
/// <c>social-cards/getting-started.png</c>, and <c>social-cards/getting-started.txt</c> says what it shows. A page
/// with no such pair, and the root, wear the site's own picture. What it found for a route it keeps, as the handler
/// keeps a rendered page.
/// </summary>
/// <remarks>
/// Convention over configuration: a file where the rule says it is makes the card, so a site gives a page its card by
/// adding two files and no line of code. Louis picked it on 2026-11-22 (F169.6), over a page or a menu entry that
/// names its picture. The price is a rule a person has to know, which the site's design doc says. A picture with no
/// words beside it is no card, so a picture never shows with another picture's words.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0055-keep-it-simple.md">ADR 0055: keep it simple</seealso>
public sealed class PageCards(IFileProvider webRoot)
{
    /// <summary>The folder of the cards in the web root.</summary>
    public const string Folder = "social-cards";

    /// <summary>A card's picture's ending.</summary>
    public const string PictureEnding = ".png";

    /// <summary>The ending of the file that says what a card shows.</summary>
    public const string WordsEnding = ".txt";

    private readonly ConcurrentDictionary<string, PageCard?> _found = new();

    /// <summary>The card of the page at this route, or none when it wears the site's own picture.</summary>
    public PageCard? For(string route) => _found.GetOrAdd(route, Find);

    /// <summary>The two files named for the route, read once.</summary>
    private PageCard? Find(string route)
    {
        string name = route.Trim('/');
        if (name.Length == 0) return null;

        string picture = $"{Folder}/{name}{PictureEnding}";
        var words = webRoot.GetFileInfo($"{Folder}/{name}{WordsEnding}");
        if (!webRoot.GetFileInfo(picture).Exists || !words.Exists) return null;

        using var stream = words.CreateReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return new PageCard(picture, reader.ReadToEnd().Trim());
    }
}
