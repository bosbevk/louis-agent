using LouisAgent.Composition.Logging;
using LouisAgent.Packs.Recipes.Messages;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Packs.Recipes.Sources.Gathering;

/// <summary>
/// One search over several recipe sites. Ask for a dish and it picks a few sites at random and asks them at once; you
/// get their hits back taking turns.
/// </summary>
/// <remarks>
/// Scatter-Gather: one request goes to several places and the answers are merged into one. The cook used to name a
/// site, and it named the same one nearly every time, so the choice of site moved here, where it can't settle on a
/// favourite. A site that refuses is skipped, so one small site can't sink the search. Taking turns keeps each site's
/// own best guess near the top, which a plain shuffle of all the hits would lose.
/// </remarks>
/// <seealso href="https://www.enterpriseintegrationpatterns.com/patterns/messaging/BroadcastAggregate.html">Scatter-Gather</seealso>
public sealed class RecipeScatter(IReadOnlyCollection<IRecipeSource> sources, Random random)
{
    private static readonly ILogger Log = PackLog.For<RecipeScatter>();

    /// <summary>Four, because the browser opens at most four pages at once.</summary>
    private const int SitesAsked = 4;

    /// <summary>The hits of a few sites picked at random, at most <paramref name="maxResults"/> of them.</summary>
    public async Task<RecipeGather> FindAsync(string query, int maxResults,
        CancellationToken cancellationToken = default)
    {
        IRecipeSource[] shuffled = [.. sources];
        random.Shuffle(shuffled);
        var drawn = shuffled.Take(SitesAsked).ToList();

        // All at once, so one search costs about as long as its slowest site, not all of them added up.
        var answers = await Task.WhenAll(drawn.Select(site => AskAsync(site, query, maxResults, cancellationToken)));

        // Each site's first hit, then each site's second: the sort keeps the drawn order within a turn.
        var hits = answers
            .SelectMany(answer =>
                answer.Hits.Select((hit, turn) => (Turn: turn, Hit: new SiteHit(answer.Site.Site, hit))))
            .OrderBy(entry => entry.Turn)
            .Take(maxResults)
            .Select(entry => entry.Hit)
            .ToList();
        return new RecipeGather(hits, [.. drawn.Select(site => site.Site)],
            [.. answers.Select(answer => answer.Refusal).OfType<string>()]);
    }

    /// <summary>One site's answer, with its refusal as a value, so it can't take the other sites down.</summary>
    private static async Task<SiteAnswer> AskAsync(IRecipeSource site, string query, int maxResults,
        CancellationToken cancellationToken)
    {
        try
        {
            return new SiteAnswer(site, await site.FindAsync(query, maxResults, cancellationToken), null);
        }
        catch (RecipeSourceException ex)
        {
            // Only the site's name in the log: the refusal's text holds the user's words.
            Log.Log(LogLevel.Warning, RecipeLogMessage.SiteSkipped, new { site.Site.Slug });
            return new SiteAnswer(site, [], ex.Message);
        }
    }

    /// <summary>What one site said: its hits, or why it refused.</summary>
    private sealed record SiteAnswer(IRecipeSource Site, IReadOnlyList<RecipeHit> Hits, string? Refusal);
}
