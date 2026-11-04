using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Web.Search;
using LouisAgent.Composition.Web.Browsing;
using LouisAgent.Choices;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Search;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Providers;
using LouisAgent.Packs.Recipes.Sources.Sites;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Sources;

/// <summary>
/// Picks the sites a deployment turned on and gives each the search its row names. Nothing is on until the setting
/// names it, so a deployment that hasn't checked a site's terms doesn't read it (ADR 0023).
/// </summary>
/// <remarks>
/// The factory that picks the Strategy (GoF) for each site from its row's <c>search</c> column, in one place and one
/// switch, so nothing else branches on how a site is searched (ADR 0003). It is the pack's counterpart of the shops'
/// <c>ShopCatalogueFactory</c>.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0003-provider-strategies.md">ADR 0003: provider strategies</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0023-copyleft-service-beside-us.md">ADR 0023: a copyleft service may run beside us</seealso>
public static class RecipeSourceFactory
{
    private static readonly ILogger Log = PackLog.For<LogCategory>();

    /// <summary>
    /// The sites <c>RECIPE_SOURCES</c> turns on: slugs separated by commas, case ignored, each once, or <c>*</c> for
    /// every site. A slug that is no site is ignored, and said so once in the log.
    /// </summary>
    public static IReadOnlyList<RecipeSite> Enabled(string? setting)
    {
        string[] names =
            (setting ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (names.Contains("*")) return RecipeSites.All;

        var unknown = names.Where(name => RecipeSites.Find(name) is null).Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (string name in unknown) Log.Log(LogLevel.Warning, RecipeLogMessage.SourceUnknown, new { Slug = name });

        return [.. names.Select(RecipeSites.Find).OfType<RecipeSite>().DistinctBy(site => site.Slug)];
    }

    /// <summary>A search for each enabled site, found by the site's slug.</summary>
    public static IReadOnlyDictionary<string, IRecipeSource> Create(IReadOnlyList<RecipeSite> enabled,
        IWebBrowser browser, IWebSearchProvider webSearch) =>
        enabled.DistinctBy(site => site.Slug).ToDictionary(site => site.Slug, site => site.Search switch
        {
            RecipeSearch.WordPress => (IRecipeSource)new WordPressSearch(site, browser),
            RecipeSearch.Web => new SiteWebSearch(site, webSearch),
            _ => throw new ArgumentOutOfRangeException(nameof(enabled), site.Search, null),
        });

    /// <summary>The name the log lines of this class are filed under.</summary>
    private sealed class LogCategory;
}
