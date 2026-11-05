using LouisAgent.Composition.Logging;
using LouisAgent.Packs.Recipes.Library.Feed;
using LouisAgent.Packs.Recipes.Library.Store;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Sources.Sites;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Library;

/// <summary>
/// The library as the tools use it: look for a page, search, and keep a new read. It holds the rules in one place: only
/// a page from one of our sites is kept, a library that's down costs nothing, and an eval's trial leaves it alone.
/// </summary>
/// <remarks>
/// Facade over the two halves, so a read and a search never deal with a store, a feed, a clock or a failure. Only our
/// own sites' pages enter because <c>ReadRecipe</c> reads any public page, and a stored page reaches other people's chats
/// and the open feed, so a page from an unknown site could carry instructions to them.
/// </remarks>
/// <param name="library">Where it reads from: the open feed.</param>
/// <param name="store">Where it keeps new reads.</param>
/// <param name="enabled">The sites whose pages may enter.</param>
/// <param name="clock">When a read happened.</param>
/// <param name="skip">Whether to leave the library alone right now, as an eval's trial does; null never skips.</param>
/// <seealso href="https://refactoring.guru/design-patterns/facade">Facade</seealso>
public sealed class RecipeShelf(
    IRecipeLibrary library,
    IRecipeStore store,
    IReadOnlyList<RecipeSite> enabled,
    TimeProvider clock,
    Func<bool>? skip = null)
{
    private static readonly ILogger Log = PackLog.For<RecipeShelf>();

    /// <summary>A shelf with nothing on it, for where no library is set up.</summary>
    public static RecipeShelf Empty { get; } =
        new(new NoRecipeLibrary(), new NoRecipeStore(), [], TimeProvider.System);

    /// <summary>The page, if we hold it.</summary>
    public async Task<StoredRecipe?> FindAsync(Uri page, CancellationToken cancellationToken) =>
        await QuietlyAsync(() => library.FindAsync(page, cancellationToken), null, cancellationToken);

    /// <summary>The pages we hold that answer a search.</summary>
    public async Task<IReadOnlyList<StoredRecipe>> SearchAsync(string query, int most, CancellationToken cancellationToken) =>
        await QuietlyAsync(() => library.SearchAsync(query, most, cancellationToken), [], cancellationToken);

    /// <summary>Keeps a read if it's from one of our sites. The store's id stops a page entering twice.</summary>
    public async Task KeepAsync(Uri page, string title, string print, CancellationToken cancellationToken)
    {
        if (SiteOf(page) is not { } site) return;

        var recipe = new StoredRecipe(page, site.Slug, title, print, clock.GetUtcNow());
        await QuietlyAsync<object?>(async () =>
        {
            await store.KeepAsync(recipe, cancellationToken);
            return null;
        }, null, cancellationToken);
    }

    /// <summary>The enabled site this page is on, or null when it's on none of them.</summary>
    private RecipeSite? SiteOf(Uri page)
    {
        string host = RecipeAddress.Of(new Uri(page.GetLeftPart(UriPartial.Authority)));
        return enabled.FirstOrDefault(site => RecipeAddress.Of(new Uri(site.Home.GetLeftPart(UriPartial.Authority))) == host);
    }

    /// <summary>Does the work unless told to skip, and logs a failure and gives the fallback.</summary>
    private async Task<T> QuietlyAsync<T>(Func<Task<T>> work, T fallback, CancellationToken cancellationToken)
    {
        if (skip?.Invoke() == true) return fallback;

        try
        {
            return await work();
        }
        // The recipe matters more than the library, so its failure never reaches the cook; a cancel still does.
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Log(LogLevel.Warning, RecipeLogMessage.LibraryUnavailable, new { Reason = ex.GetType().Name });
            return fallback;
        }
    }
}
