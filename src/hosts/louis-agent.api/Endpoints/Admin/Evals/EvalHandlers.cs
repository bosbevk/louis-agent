using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Endpoints.Admin.Runs;
using LouisAgent.Api.Evals.Drafts.Store;
using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Store;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Endpoints.Admin.Evals;

/// <summary>
/// The admin's eval list: every eval the runner's catalogue has, each with its runs from the store and how many drafts
/// wait for it. An eval that never ran is still listed, and says so.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>AdminHandlers</c>: the route
/// shares the catalogue, the stores and the workspace, so it can be a named method a test calls with no HTTP. It
/// returns <c>TypedResults</c> with its answer declared.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class EvalHandlers(EvalList catalog, IEvalStore? store, string workspaceRoot, IEvalDraftStore drafts)
{
    /// <summary>How many of an eval's newest runs the list carries.</summary>
    private const int RunsPerEval = 50;

    /// <summary>
    /// Every eval with its runs and its drafts counted. Without a store, or when it doesn't answer, the evals are still
    /// listed and the store's banner says why there are no runs.
    /// </summary>
    public async Task<JsonHttpResult<EvalListReply>> GetList(CancellationToken cancellationToken)
    {
        var counts = await DraftCountsAsync(cancellationToken);

        // No store: list the evals anyway, with no runs and no error to show.
        if (store is null) return ListOf(null, null, counts);

        try
        {
            return ListOf(await RunsAsync(store, cancellationToken), null, counts);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The page still loads: it shows the evals and says why their runs aren't there.
            return ListOf(null, AdminEndpoints.Unavailable(ex), counts);
        }
    }

    /// <summary>Each eval's newest runs, by the eval's name.</summary>
    private async Task<IReadOnlyDictionary<string, IReadOnlyList<EvalDocuments.Run>>> RunsAsync(
        IEvalStore runs, CancellationToken cancellationToken)
    {
        // One read of the store per eval in the catalogue, each capped at its newest runs.
        var byEval = new Dictionary<string, IReadOnlyList<EvalDocuments.Run>>(StringComparer.Ordinal);
        foreach (var check in catalog.Checks)
            byEval[check.Id] = await runs.ListRunsAsync(check.Id, RunsPerEval, cancellationToken);
        return byEval;
    }

    /// <summary>
    /// How many drafts wait for each eval, samples and transcripts together; none when the draft store doesn't
    /// answer, since the list matters more than the count.
    /// </summary>
    private async Task<IReadOnlyDictionary<string, int>?> DraftCountsAsync(CancellationToken cancellationToken)
    {
        try
        {
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var check in catalog.Checks)
            {
                var of = await drafts.OfEvalAsync(check.Id, cancellationToken);
                counts[check.Id] = of.Samples.Count + of.Transcripts.Count;
            }

            return counts;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The runs' store banner already says when Mongo is down; the counts just stay at none.
            return null;
        }
    }

    /// <summary>The reply for these runs and draft counts, with the store's state beside them.</summary>
    private JsonHttpResult<EvalListReply> ListOf(
        IReadOnlyDictionary<string, IReadOnlyList<EvalDocuments.Run>>? runs, string? error,
        IReadOnlyDictionary<string, int>? counts) =>
        TypedResults.Json(
            EvalListReply.Of(catalog, workspaceRoot, runs, StoreRow.Of(store is not null, error), counts),
            AdminJson.SnakeCase);
}
