using System.Globalization;
using LouisAgent.Composition.Messages;
using LouisAgent.FilmExport.Cards.Pages;
using LouisAgent.FilmExport.Messages;
using LouisAgent.FilmExport.Messages.Holes.Cards;
using LouisAgent.FilmExport.Rendering;
using PuppeteerSharp;

namespace LouisAgent.FilmExport;

/// <summary>
/// The <c>framework-cards</c> command (F169.6): starts the framework's site, reads its pages from its sitemap, and
/// writes a share card for each into its web root, the site's own card for the root and a page's card with the words
/// beside it for every other page. Run it after a page's name or heading changes, and commit what it writes.
/// </summary>
/// <remarks>
/// The files go where the host's <c>PageCards</c> looks: <c>social-cards/{route}.png</c> and <c>.txt</c>. The tool
/// does not reference the host, so the folder's name is known twice; the framework's site's <c>SiteCardsTests</c>
/// holds the two to one rule by asking the host for every page's card in the real web root.
/// </remarks>
internal static class FrameworkCards
{
    /// <summary>The command's name.</summary>
    public const string Command = "framework-cards";

    /// <summary>The site's name on a page's card, in points: small, above the page's own name.</summary>
    private const int PageNameSize = 44;

    /// <summary>The site's name on its own card, in points: large, the card's subject.</summary>
    private const int SiteNameSize = 132;

    /// <summary>The site's own picture in its web root, worn by the root and any page with no card.</summary>
    private const string SiteCard = "social-card.png";

    /// <summary>The folder of the pages' cards in the web root.</summary>
    private const string Folder = "social-cards";

    /// <summary>Draws every card of the framework's site, from the code as it is now.</summary>
    public static async Task RunAsync(string repoRoot, string chrome)
    {
        var site = CardSite.Framework;
        var pages = CardSite.ReadPages(File.ReadAllText(Path.Combine(repoRoot, site.Sitemap)));
        string webRoot = Path.Combine(repoRoot, site.WebRoot);
        string pageTemplate = File.ReadAllText(Path.Combine(repoRoot, PageCardPage.PageTemplatePath));
        string siteTemplate = File.ReadAllText(Path.Combine(repoRoot, PageCardPage.SiteTemplatePath));

        using var running = await SiteProcess.StartAsync(repoRoot, site.Host);
        await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            ExecutablePath = chrome
        });
        var renderer = new PageCardRenderer(browser, running.Address);
        var small = await renderer.NameAsync(PageNameSize);
        var large = await renderer.NameAsync(SiteNameSize);

        foreach (var page in pages)
        {
            // The root wears the site's own card; every other page its own, with its words beside it.
            if (page.Route == "/")
            {
                string own = Path.Combine(webRoot, SiteCard);
                Written(own, await renderer.WriteAsync(PageCardPage.ForSite(siteTemplate, large, page.Address),
                    page.Route, own));
                continue;
            }

            var words = await renderer.WordsAsync(page);
            string picture = Path.Combine(webRoot, Folder, page.Route.Trim('/') + ".png");
            Written(picture, await renderer.WriteAsync(PageCardPage.ForPage(pageTemplate, small, words),
                page.Route, picture));
            File.WriteAllText(Path.ChangeExtension(picture, ".txt"), words.Description + "\n");
        }
    }

    /// <summary>Tells the person running it where a card went, and where its words end.</summary>
    private static void Written(string path, double endsAt)
    {
        var written = new PageCardWrittenHoles(path, Math.Round(endsAt).ToString(CultureInfo.InvariantCulture),
            CardFit.Middle.ToString(CultureInfo.InvariantCulture));
        Console.WriteLine(MessageText.Format(FilmExportUserText.PageCardWritten, written));
    }
}
