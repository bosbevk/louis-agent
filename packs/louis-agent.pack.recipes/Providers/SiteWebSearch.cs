using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Sources;
using LouisAgent.Packs.Recipes.Sources.Sites;

namespace LouisAgent.Packs.Recipes.Providers;

/// <summary>
/// Searches a recipe site through the web search provider, asked for that site only. Use it for a site whose own
/// search we may not use or can't read. Ask for a word and you get the pages the provider found on the site.
/// </summary>
/// <remarks>
/// Strategy (GoF), picked by the row's <c>search</c> column (ADR 0003), beside <see cref="WordPressSearch"/>; one
/// class serves every such site because only the row differs (Refactoring.Guru: <i>Replace Subclass with
/// Fields</i>). The provider's <c>site:</c> limit is a hint, not a promise, so a result on another host is dropped
/// here. Each query costs what the provider charges (Tavily on the sold endpoint, SearXNG for development), and the
/// provider's own limits hold. The provider is limited by host, the one thing they all take, so a home with a path
/// (Rooi Rose lives under <c>novanews.co.za/rooirose</c>) is held here too: without that, the publisher's other
/// sections would be listed as Rooi Rose.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
public sealed class SiteWebSearch(RecipeSite site, IWebSearchProvider search) : IRecipeSource
{
    /// <inheritdoc />
    public RecipeSite Site => site;

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecipeHit>> FindAsync(string query, int maxResults,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WebSearchResult> results;
        try
        {
            results = await search.SearchAsync(new WebSearchQuery(query, maxResults, SearchResultLinks.Host(site.Home)),
                cancellationToken);
        }
        catch (WebSearchException ex)
        {
            throw new RecipeSourceException(MessageText.Format(RecipeToolMessage.SourceRefused,
                new
                {
                    Source = site.Name,
                    Query = query,
                    Reason = ex.Message
                }));
        }

        return
        [
            .. results
                .Select(result => (result.Title, Url: Parse(result.Url)))
                .Where(result => result.Url is not null && OnTheSite(result.Url))
                .Select(result => new RecipeHit(result.Title, result.Url!))
        ];
    }

    /// <summary>The result's address as a web address, or null when it isn't one.</summary>
    private static Uri? Parse(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed) && parsed.Scheme is "http" or "https" ? parsed : null;

    /// <summary>Whether the result is on the site's host, with or without <c>www.</c>, and under its home.</summary>
    private bool OnTheSite(Uri url) =>
        SearchResultLinks.Host(url) == SearchResultLinks.Host(site.Home) && UnderHome(url);

    /// <summary>Whether it's at or below the home's path; the slash keeps <c>/rooirose-extra</c> out.</summary>
    private bool UnderHome(Uri url) =>
        (url.AbsolutePath.TrimEnd('/') + "/").StartsWith(site.Home.AbsolutePath.TrimEnd('/') + "/",
            StringComparison.Ordinal);
}
