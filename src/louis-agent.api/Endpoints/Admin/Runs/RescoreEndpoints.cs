using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// Hangs the re-score route, a verb on the stored run (ADR 0039), which grades a run's transcripts again with today's
/// graders. The work is in <c>RescoreHandlers</c>.
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
