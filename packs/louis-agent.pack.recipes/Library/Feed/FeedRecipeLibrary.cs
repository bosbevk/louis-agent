using System.Net;
using LouisAgent.Choices;
using LouisAgent.Composition.Logging;
using LouisAgent.Packs.Recipes.Messages;
using LouisAgent.Packs.Recipes.Providers;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Library.Feed;

/// <summary>
/// The library as a source: it reads the open feed over HTTP, with no key, as anyone would, holds what it read for five
/// minutes, and searches that with the same word rule as the sites' hits. A feed that's down has nothing to offer, and
/// nothing breaks.
/// </summary>
/// <remarks>
/// Reading our own open API on purpose (Louis's pick): its first customer is ourselves. It uses its own
/// <see cref="HttpClient"/> to a set address, as a peer does, because the guarded fetcher is for addresses a model
/// gives us, and it would rightly refuse our own. A failure is held for a minute, so a feed that's down isn't asked on
/// every call.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0070-packs-publish-data-feeds.md">ADR 0070: packs publish data feeds</seealso>
public sealed class FeedRecipeLibrary(HttpClient http, Uri api, TimeProvider clock) : IRecipeLibrary
{
    private static readonly ILogger Log = PackLog.For<FeedRecipeLibrary>();
    private static readonly TimeSpan HoldFor = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan HoldAFailureFor = TimeSpan.FromMinutes(1);

    /// <summary>How long a read of the feed may take: a slow feed must not hold up a search.</summary>
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(3);

    private readonly SemaphoreSlim _reading = new(1, 1);
    private IReadOnlyList<StoredRecipe> _held = [];
    private DateTimeOffset _freshUntil = DateTimeOffset.MinValue;

    /// <summary>Where the feed is on the API.</summary>
    private readonly Uri _feed = new(api, $"v1/feeds/{RecipesToolset.Recipes.Wire()}");

    /// <inheritdoc />
    public Uri AddressOf(StoredRecipe recipe) => new($"{_feed.AbsoluteUri}/{recipe.Id}");

    /// <summary>
    /// The id, only for exactly our own recipes path and a real id. The model hands us this address, so nothing else
    /// may count as ours, and the request is built again from our setting, never from what it gave.
    /// </summary>
    public string? IdIn(Uri address)
    {
        string start = _feed.AbsoluteUri + "/";
        if (!address.AbsoluteUri.StartsWith(start, StringComparison.Ordinal)) return null;

        string id = address.AbsoluteUri[start.Length..];
        return RecipeAddress.IsId(id) ? id : null;
    }

    /// <summary>
    /// Reads one recipe from its own address, every time, so a page kept a moment ago can be read. A failure reaches
    /// the shelf, which says so.
    /// </summary>
    public async Task<StoredRecipe?> ReadAsync(string id, CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Limit);

        using var response = await http.GetAsync(new Uri($"{_feed.AbsoluteUri}/{id}"), limit.Token);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();
        return RecipeFeedRow.Read(await response.Content.ReadAsStringAsync(limit.Token)).ToRecipe();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StoredRecipe>> SearchAsync(string query, int most, CancellationToken cancellationToken) =>
    [
        .. (await HeldAsync(cancellationToken)).Where(recipe => QueryWords.Answers(recipe.Title, recipe.Page, query)).Take(most)
    ];

    /// <summary>What the feed held when last read, read again once that's gone stale. One reader at a time.</summary>
    private async Task<IReadOnlyList<StoredRecipe>> HeldAsync(CancellationToken cancellationToken)
    {
        if (clock.GetUtcNow() < _freshUntil) return _held;

        await _reading.WaitAsync(cancellationToken);
        try
        {
            if (clock.GetUtcNow() >= _freshUntil) await ReadTheFeedAsync(cancellationToken);
            return _held;
        }
        finally
        {
            _reading.Release();
        }
    }

    /// <summary>Reads the feed. On a failure it keeps what it had, logs it, and tries again in a minute.</summary>
    private async Task ReadTheFeedAsync(CancellationToken cancellationToken)
    {
        using var limit = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        limit.CancelAfter(Limit);
        try
        {
            string json = await http.GetStringAsync(_feed, limit.Token);
            _held = [.. RecipeFeedReply.Read(json).Recipes.Select(row => row.ToRecipe())];
            _freshUntil = clock.GetUtcNow() + HoldFor;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Log(LogLevel.Warning, RecipeLogMessage.LibraryUnavailable, new { Reason = ex.GetType().Name });
            _freshUntil = clock.GetUtcNow() + HoldAFailureFor;
        }
    }
}
