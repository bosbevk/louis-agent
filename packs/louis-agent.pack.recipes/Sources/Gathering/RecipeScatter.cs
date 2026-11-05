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

    /// <summary>How far down a site's list its first place can come from: its own best guesses, in any order.</summary>
    private const int TopHits = 3;

    /// <summary>
    /// The hits of a few sites picked at random, at most <paramref name="maxResults"/> of them, leaving out the sites
    /// to <paramref name="skip"/> (by name or short name) unless that leaves none to ask. Nothing is remembered here:
    /// the caller passes what it has already asked, so the next dish starts with every site back in the draw.
    /// </summary>
    public async Task<RecipeGather> FindAsync(string query, int maxResults,
        IReadOnlyCollection<string>? skip = null, CancellationToken cancellationToken = default)
    {
        var left = sources.Where(source => !Skipped(source, skip)).ToArray();
        IRecipeSource[] shuffled = left.Length > 0 ? left : [.. sources];
        random.Shuffle(shuffled);
        var drawn = shuffled.Take(SitesAsked).ToList();

        // All at once, so one search costs about as long as its slowest site, not all of them added up.
        var answers = await Task.WhenAll(drawn.Select(site => AskAsync(site, query, maxResults, cancellationToken)));

        // Each site's first hit, then each site's second: the sort keeps the drawn order within a turn.
        var hits = answers
            .SelectMany(answer =>
                Mixed(answer.Hits).Select((hit, turn) => (Turn: turn, Hit: new SiteHit(answer.Site.Site, hit))))
            .OrderBy(entry => entry.Turn)
            .Take(maxResults)
            .Select(entry => entry.Hit)
            .ToList();
        return new RecipeGather(hits, [.. drawn.Select(site => site.Site)],
            [.. answers.Select(answer => answer.Refusal).OfType<string>()]);
    }

    /// <summary>Whether a site is one to leave out; the words given may be its name or its short name.</summary>
    private static bool Skipped(IRecipeSource source, IReadOnlyCollection<string>? skip) =>
        skip is not null && skip.Any(name =>
            name.Equals(source.Site.Slug, StringComparison.OrdinalIgnoreCase) ||
            name.Equals(source.Site.Name, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// A site's hits with its top few in any order, so the same site doesn't give the same page every time; the rest
    /// stay as the site lists them. It stores nothing, so no page is ever kept out.
    /// </summary>
    private IEnumerable<RecipeHit> Mixed(IReadOnlyList<RecipeHit> hits)
    {
        RecipeHit[] top = [.. hits.Take(TopHits)];
        random.Shuffle(top);
        return top.Concat(hits.Skip(TopHits));
    }

    /// <summary>
    /// One site's answer, with its refusal as a value, so it can't take the other sites down. A page that hangs counts
    /// as a refusal, since the browser can time out in places its own failure doesn't cover.
    /// </summary>
    private static async Task<SiteAnswer> AskAsync(IRecipeSource site, string query, int maxResults,
        CancellationToken cancellationToken)
    {
        try
        {
            return new SiteAnswer(site, await site.FindAsync(query, maxResults, cancellationToken), null);
        }
        catch (Exception ex) when (ex is RecipeSourceException or TimeoutException)
        {
            // Only the site's name in the log: the refusal's text holds the user's words.
            Log.Log(LogLevel.Warning, RecipeLogMessage.SiteSkipped, new { site.Site.Slug });
            return new SiteAnswer(site, [], ex.Message);
        }
    }

    /// <summary>What one site said: its hits, or why it refused.</summary>
    private sealed record SiteAnswer(IRecipeSource Site, IReadOnlyList<RecipeHit> Hits, string? Refusal);
}
