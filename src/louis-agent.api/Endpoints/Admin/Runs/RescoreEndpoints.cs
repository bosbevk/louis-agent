using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// <c>POST /v1/admin/benchmarks/runs/{id}/rescore</c> (F18 step 5b.2, ADR 0039: a verb on a resource): grades a stored
/// run's transcripts again with today's graders. The work is <c>RescoreHandlers</c>'.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class RescoreEndpoints
{
    /// <summary>Adds the re-score route to the admin group.</summary>
    public static void MapRescore(this RouteGroupBuilder admin, BenchmarkCatalog catalog, BenchmarkRunner runner,
        IBenchmarkStore? store, BenchmarkRecorder? recorder,
        Func<BenchmarkCheck, IReadOnlyDictionary<string, string>> settingsOf)
    {
        var handlers = new RescoreHandlers(catalog, runner, store, recorder, settingsOf);

        admin.MapPost("/benchmarks/runs/{id}/rescore", handlers.Rescore);
    }
}
