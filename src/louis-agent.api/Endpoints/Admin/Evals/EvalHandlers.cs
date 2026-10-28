using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Endpoints.Admin.Runs;
using Microsoft.AspNetCore.Http.HttpResults;

namespace LouisAgent.Api.Endpoints.Admin.Evals;

/// <summary>
/// The admin's eval list: every eval the runner's catalogue has, each with its runs from the store. An eval that never
/// ran is still listed, and says so.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>AdminHandlers</c>: the route
/// shares the catalogue, the store and the workspace, so it can be a named method a test calls with no HTTP. It returns
/// <c>TypedResults</c> with its answer declared.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class EvalHandlers(BenchmarkCatalog catalog, IBenchmarkStore? store, string workspaceRoot)
{
    /// <summary>How many of an eval's newest runs the list carries.</summary>
    private const int RunsPerEval = 50;

    /// <summary>
    /// Every eval with its runs. Without a store, or when it doesn't answer, the evals are still listed and the store's
    /// banner says why there are no runs.
    /// </summary>
    public async Task<JsonHttpResult<EvalListReply>> GetList(CancellationToken cancellationToken)
    {
        // No store: list the evals anyway, with no runs and no error to show.
        if (store is null) return ListOf(null, null);

        try
        {
            return ListOf(await RunsAsync(store, cancellationToken), null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The page still loads: it shows the evals and says why their runs aren't there.
            return ListOf(null, AdminEndpoints.Unavailable(ex));
        }
    }

    /// <summary>Each eval's newest runs, by the eval's name.</summary>
    private async Task<IReadOnlyDictionary<string, IReadOnlyList<BenchmarkDocuments.Run>>> RunsAsync(
        IBenchmarkStore runs, CancellationToken cancellationToken)
    {
        // One read of the store per eval in the catalogue, each capped at its newest runs.
        var byEval = new Dictionary<string, IReadOnlyList<BenchmarkDocuments.Run>>(StringComparer.Ordinal);
        foreach (var check in catalog.Checks)
            byEval[check.Id] = await runs.ListRunsAsync(check.Id, RunsPerEval, cancellationToken);
        return byEval;
    }

    /// <summary>The reply for these runs, with the store's state beside them.</summary>
    private JsonHttpResult<EvalListReply> ListOf(
        IReadOnlyDictionary<string, IReadOnlyList<BenchmarkDocuments.Run>>? runs, string? error) =>
        TypedResults.Json(EvalListReply.Of(catalog, workspaceRoot, runs, StoreRow.Of(store is not null, error)),
            AdminJson.SnakeCase);
}
