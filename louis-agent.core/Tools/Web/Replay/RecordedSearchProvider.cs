using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Replay.Store;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Core.Tools.Web.Replay;

/// <summary>
/// A search provider, recorded or replayed (F18 step 4b), by <see cref="ReplayTrial.Current"/> on each call: no trial passes through and
/// saves nothing; Record saves the results under the trial and returns them unchanged, a <see cref="WebSearchException"/> as its text (and
/// rethrown; the model sees that text), any other exception and a cancel not at all; Replay never calls the inner provider, answers from
/// the first recording of the ask, rethrows a recorded failure's text, and a miss as a text that names the query (ADR 0005). The key is the
/// provider's name and the query's fields; the request to the provider, and so its key, is never saved.
/// </summary>
public sealed class RecordedSearchProvider(IWebSearchProvider inner, WebReplayStore store) : IWebSearchProvider
{
    public string Name => inner.Name;

    /// <summary><paramref name="inner"/> as it is where no store is kept, else recorded around.</summary>
    public static IWebSearchProvider Around(IWebSearchProvider inner, WebReplayStore? store) =>
        store is null ? inner : new RecordedSearchProvider(inner, store);

    public async Task<IReadOnlyList<WebSearchResult>> SearchAsync(WebSearchQuery query, CancellationToken cancellationToken = default)
    {
        if (ReplayTrial.Current is not { } trial) return await inner.SearchAsync(query, cancellationToken);

        var ask = ReplayAsk.Of(ReplayKind.Search, new { Provider = inner.Name, query.Text, query.MaxResults, query.Site, query.Recency });
        return trial.Replaying
            ? await ReplayAsync(trial.Of, ask, query, cancellationToken)
            : await RecordAsync(trial.Of, ask, query, cancellationToken);
    }

    private async Task<IReadOnlyList<WebSearchResult>> RecordAsync(string trial, ReplayAsk ask, WebSearchQuery query, CancellationToken cancellationToken)
    {
        IReadOnlyList<WebSearchResult> results;
        try
        {
            results = await inner.SearchAsync(query, cancellationToken);
        }
        catch (WebSearchException failure)
        {
            await store.SaveFailureAsync(trial, ask, failure.Message, cancellationToken);
            throw;
        }
        await store.SaveAnswerAsync(trial, ask, results, cancellationToken);
        return results;
    }

    private async Task<IReadOnlyList<WebSearchResult>> ReplayAsync(string trial, ReplayAsk ask, WebSearchQuery query, CancellationToken cancellationToken)
    {
        if (await store.FindAsync<IReadOnlyList<WebSearchResult>>(trial, ask, cancellationToken) is not { } recorded)
            throw new WebSearchException(MessageText.Format(WebToolMessage.ReplaySearchMiss, new { Query = query.Text }));
        return recorded.OrThrow(error => new WebSearchException(error));
    }
}
