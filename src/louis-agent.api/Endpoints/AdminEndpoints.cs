using System.Net.ServerSentEvents;
using LouisAgent.Api.Messages;
using LouisAgent.Api.Services;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The admin portal's endpoints (F27), all under <c>/admin</c> and all behind <see cref="AdminGate"/>: with no admin
/// key configured every call is 403 and says what to set; with one, a call must present it. Step 1: the checks and
/// the saved runs; the runs themselves come in step 3.
/// </summary>
internal static class AdminEndpoints
{
    public static void MapAdmin(this WebApplication app, BenchmarkCatalog catalog, BenchmarkRunner runner, string? adminKey)
    {
        var admin = app.MapGroup("/admin").AddEndpointFilter(async (context, next) =>
        {
            switch (AdminGate.Check(adminKey, AdminGate.Presented(context.HttpContext.Request)))
            {
                case AdminAccess.Closed:
                    return ApiErrors.Result(StatusCodes.Status403Forbidden, "permission_error", MessageText.Format(ApiUserText.AdminClosed));
                case AdminAccess.Unauthorized:
                    return ApiErrors.Result(StatusCodes.Status401Unauthorized, "authentication_error", MessageText.Format(ApiUserText.InvalidApiKey));
                default:
                    return await next(context);
            }
        });

        admin.MapGet("/benchmarks", () => Results.Json(new
        {
            checks = catalog.Checks.Select(c => new { id = c.Id, title = c.Title, script = c.Script }),
            runs = catalog.SavedRuns().Select(Row),
        }));

        // Step 3: a run from the page. 202 with the run; 409 while one runs; its lines as SSE, then "done".
        admin.MapPost("/benchmarks/{check}/runs", (string check, StartRunRequest? request) =>
        {
            if (catalog.Checks.All(c => c.Id != check))
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
                    MessageText.Format(ApiUserText.NoCheck, new { Check = check, Checks = string.Join(", ", catalog.Checks.Select(c => c.Id)) }));
            if (runner.IsRunning)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Folder }));
            return runner.Start(check, request?.Repeat ?? 1) is { } run
                ? Results.Json(Row(run), statusCode: StatusCodes.Status202Accepted)
                : ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Folder }));
        });

        // The run in progress, or the last one, so a reloaded page can pick it up again.
        admin.MapGet("/benchmarks/runs/current", () => runner.Current is { } run
            ? Results.Json(Row(run))
            : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoBenchmarkRun)));

        admin.MapGet("/benchmarks/runs/{id}/events", (string id, CancellationToken cancellationToken) =>
            runner.Current is { } run && run.Id == id
                ? (IResult)TypedResults.ServerSentEvents(Events(runner, run, cancellationToken))
                : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoBenchmarkRun)));

        admin.MapGet("/benchmarks/runs/{folder}", (string folder) => catalog.Read(folder) is { } run
            ? Results.Json(new
            {
                run = Row(run.Run), summary = run.Summary, readme = run.Readme, results = run.Results,
                transcripts = run.Transcripts.Select(t => new { name = t.Name, markdown = t.Markdown }),
                modes = run.Modes.Select(m => new { name = m.Name, usage = m.Usage, answers = m.Answers }),
            })
            : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoRun, new { Folder = folder })));
    }

    private static async IAsyncEnumerable<SseItem<string>> Events(BenchmarkRunner runner, BenchmarkRun run,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (string line in runner.EventsAsync(run.Id, cancellationToken))
            yield return new SseItem<string>(line, "line");
        yield return new SseItem<string>(System.Text.Json.JsonSerializer.Serialize(Row(run)), "done");
    }

    private static object Row(SavedRun run) =>
        new { folder = run.Folder, check = run.Check, saved_at = run.SavedAt, repeats = run.Repeats, cost = run.Cost };

    private static object Row(BenchmarkRun run) => new
    {
        id = run.Id, check = run.Check, folder = run.Folder, repeat = run.Repeat, status = run.Status.ToString().ToLowerInvariant(),
        exit_code = run.ExitCode, started_at = run.StartedAt, ended_at = run.EndedAt, lines = run.Lines.Count,
    };
}

/// <summary>The body of <c>POST /admin/benchmarks/{check}/runs</c>: how many repeats (1 to the runner's cap).</summary>
internal sealed record StartRunRequest(int? Repeat);
