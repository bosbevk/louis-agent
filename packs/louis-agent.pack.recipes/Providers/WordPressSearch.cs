using System.Net;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Providers;

/// <summary>
/// Searches a recipe site through its own WordPress search page (<c>/?s=</c>), opened in the guarded browser like any
/// reader's. Ask for a word and you get the pages the site lists for it; if the site refuses, you get a message that
/// says so.
/// </summary>
/// <remarks>
/// Strategy (GoF), picked by the row's <c>search</c> column (ADR 0003). One class serves every WordPress site,
/// built with the site's row, and not one class per site: the sites differ only in a name and an address, which is
/// Refactoring.Guru's <i>Replace Subclass with Fields</i>; a new WordPress site is a new row in the table. The
/// browser already speaks for us honestly, and applies the robots policy and the page signals on every page
/// (ADR 0038, ADR 0068), so this class adds nothing there.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0038-the-browser-says-what-it-is.md">ADR 0038: the browser says what it is</seealso>
public sealed class WordPressSearch(RecipeSite site, IWebBrowser browser) : IRecipeSource
{
    /// <inheritdoc />
    public RecipeSite Site => site;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeHit>> FindAsync(string query, int maxResults,
        CancellationToken cancellationToken = default)
    {
        // The search page of the site, with the words made safe for an address ("hoender kerrie" is hoender+kerrie).
        var search = new Uri($"{site.Home.AbsoluteUri.TrimEnd('/')}/?s={WebUtility.UrlEncode(query)}");
        BrowserVisit visit;
        try
        {
            visit = await browser.VisitAsync(search, cancellationToken: cancellationToken);
        }
        catch (WebBrowserException ex)
        {
            throw new RecipeSourceException(MessageText.Format(RecipeToolMessage.SourceRefused,
                new
                {
                    Source = site.Name,
                    Query = query,
                    Reason = ex.Message
                }));
        }

        return SearchResultLinks.In(visit.Html, site.Home, maxResults);
    }
}
