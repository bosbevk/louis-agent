using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Choices;
using LouisAgent.Packs.Recipes.Library.Store;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Sources.Sites;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Library.Feed;

/// <summary>
/// The library, open to anyone at <c>/v1/feeds/recipes</c>: every kept recipe, with its site's name and link. Taking a site
/// out of the table stops its recipes here at once, and our own cook reads this feed like anyone else.
/// </summary>
/// <remarks>
/// A plug-in of the contract's <see cref="IDataFeed"/> (Plugin, Fowler): the API lists and serves it without knowing this
/// pack, and the MCP server offers it as a resource (ADR 0072). A feed answers with what it has, so a store that's down is
/// an empty feed.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0070-packs-publish-data-feeds.md">ADR 0070: packs publish data feeds</seealso>
public sealed class RecipesFeed(IRecipeStore store, IReadOnlyList<RecipeSite> enabled) : IDataFeed
{
    private static readonly ILogger Log = PackLog.For<RecipesFeed>();

    /// <inheritdoc />
    public string Name => RecipesToolset.Recipes.Wire();

    /// <inheritdoc />
    public string Description => MessageText.Format(RecipeFeedMessage.Description);

    /// <inheritdoc />
    public async Task<string> ReadJsonAsync(CancellationToken cancellationToken)
    {
        var sites = enabled.ToDictionary(site => site.Slug);
        var rows = (await KeptAsync(cancellationToken))
            .Where(recipe => sites.ContainsKey(recipe.SiteSlug))
            .Select(recipe => RecipeFeedRow.Of(recipe, sites[recipe.SiteSlug]));
        return new RecipeFeedReply(MessageText.Format(RecipeFeedMessage.Notice), [.. rows]).Write();
    }

    /// <summary>What the store holds; nothing, and a line in the log, when it can't answer.</summary>
    private async Task<IReadOnlyList<StoredRecipe>> KeptAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await store.AllAsync(cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Log(LogLevel.Warning, RecipeLogMessage.LibraryUnavailable, new { Reason = ex.GetType().Name });
            return [];
        }
    }
}
