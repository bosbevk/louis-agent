using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Runs;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Recording;

namespace LouisAgent.Api.Endpoints.Admin.Runs;

/// <summary>
/// Hangs the re-score route, a verb on the stored run (ADR 0039), which grades a run's transcripts again with today's
/// graders. The work is in <c>RescoreHandlers</c>.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
internal static class RescoreEndpoints
{
    /// <summary>Adds the re-score route to the group the eval routes hang on.</summary>
    public static void MapRescore(this RouteGroupBuilder evals, EvalList catalog, EvalProcess runner,
        IEvalStore? store, EvalRecorder? recorder,
        Func<EvalEntry, IReadOnlyDictionary<string, string>> settingsOf)
    {
        var handlers = new RescoreHandlers(catalog, runner, store, recorder, settingsOf);

        evals.MapPost("/runs/{id}/rescore", handlers.Rescore);
    }
}
