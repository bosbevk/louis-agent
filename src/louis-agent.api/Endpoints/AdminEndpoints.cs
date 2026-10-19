using LouisAgent.Api.Admin;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Benchmarks.Reports;
using System.Net.ServerSentEvents;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The admin portal's endpoints (F27), all under <c>/admin</c> and all behind <see cref="AdminGate"/>: with no admin
/// key configured every call is 403 and says what to set; with one, a call must present it. The checks and a run of
/// one, recorded into the benchmark store as it runs (F31); without a store no run starts.
/// </summary>
internal static class AdminEndpoints
{
    /// <summary>How long a run's "done" waits for its recording to finish (the ledger join and the last writes).</summary>
    private static readonly TimeSpan RecordingWait = TimeSpan.FromSeconds(30);

    /// <summary>How many runs the Benchmarks page lists.</summary>
    private const int ListedRuns = 50;

    public static void MapAdmin(this WebApplication app, BenchmarkCatalog catalog, BenchmarkRunner runner, string? adminKey,
        IBenchmarkStore? store, BenchmarkRecorder? recorder, Func<BenchmarkCheck, IReadOnlyDictionary<string, string>> settingsOf,
        string workspaceRoot)
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

        // The page's first read (F31): the checks, whether the store is there (the page's banner), and the stored runs.
        admin.MapGet("/benchmarks", async (CancellationToken cancellationToken) =>
        {
            IReadOnlyList<BenchmarkDocuments.Run> runs = [];
            string? error = null;
            if (store is not null)
                try { runs = await store.ListRunsAsync(limit: ListedRuns, cancellationToken: cancellationToken); }
                catch (Exception ex) when (ex is not OperationCanceledException) { error = Unavailable(ex); }
            return Results.Json(new
            {
                checks = catalog.Checks.Select(c => new { id = c.Id, title = c.Title, script = c.Script, manual = BenchmarkManuals.Has(workspaceRoot, c) }),
                store = new { configured = store is not null, error },
                runs = runs.Select(RunRow),
            });
        });

        // A check's manual (step 10b): the markdown beside its script, found from the catalogue, never from the request.
        admin.MapGet("/benchmarks/{check}/manual", (string check) =>
            catalog.Checks.FirstOrDefault(c => c.Id == check) is { } known && BenchmarkManuals.Read(workspaceRoot, known) is { } markdown
                ? Results.Json(new { check = known.Id, title = known.Title, markdown })
                : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoManual, new { Check = check })));

        // A check's trend over its finished runs (step 11a): per mode, the explorer's nine rings as lines over the runs.
        admin.MapGet("/benchmarks/checks/{check}/trend", (string check, CancellationToken cancellationToken) => FromStore(store, async s =>
        {
            var trend = await BenchmarkTrends.ComputeAsync(s, check, cancellationToken: cancellationToken);
            return Results.Json(new
            {
                check = trend.Check,
                modes = trend.Modes.Select(m => new
                {
                    name = m.Name,
                    points = m.Points.Select(p => new
                    {
                        run = p.Run, at = p.At, cost = p.Cost, score = p.Score, conversations = p.Conversations,
                        tokens = p.Tokens, seconds_per_turn = p.SecondsPerTurn, cache_share = p.CacheShare, sourced = p.Sourced,
                        tools_failed = p.ToolsFailed, cost_per_answer = p.CostPerAnswer, sites = p.Sites,
                    }),
                }),
            });
        }));

        // A stored run with its conversations, their turns (without their events) and its requests: the explorer's levels.
        admin.MapGet("/benchmarks/runs/{id}", (string id, CancellationToken cancellationToken) => FromStore(store, async s =>
        {
            if (await s.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            var conversations = await s.ConversationsAsync(id, cancellationToken);
            var turns = new List<object>();
            foreach (var conversation in conversations)
                foreach (var turn in await s.TurnsAsync(id, conversation.Key, cancellationToken))
                    turns.Add(TurnRow(turn, await BenchmarkTools.OfTurnAsync(s, id, turn, cancellationToken)));
            var requests = await s.RequestsAsync(id, cancellationToken);
            return Results.Json(new
            {
                run = RunRow(run),
                settings = run.Settings,
                modes = (run.Modes ?? []).Select(m => new { name = m.Name, repeat = m.Repeat, toolsets = m.Toolsets, role = m.Role, llm = m.Llm }),
                log = run.Log,
                conversations = conversations.Select(ConversationRow),
                turns,
                requests = requests.Select(RequestRow),
            });
        }));

        // One turn's stream events, for its replay: the heaviest read, so only on demand.
        admin.MapGet("/benchmarks/runs/{id}/turns/{mode}/{repeat:int}/{task}/{turn:int}/events",
            (string id, string mode, int repeat, string task, int turn, CancellationToken cancellationToken) => FromStore(store, async s =>
            {
                var events = await s.TurnEventsAsync(id, new BenchmarkProtocol.TurnKey(mode, repeat, task, turn), cancellationToken);
                return Results.Json(new { events = events.Select(e => new { type = e.Type, data = e.Data, at = e.At }) });
            }));

        admin.MapDelete("/benchmarks/runs/{id}", (string id, CancellationToken cancellationToken) => FromStore(store, async s =>
        {
            if (runner.Current is { Status: BenchmarkRunStatus.Running } live && live.Id == id)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.StoredRunStillRunning, new { Id = id }));
            return await s.DeleteRunAsync(id, cancellationToken) ? Results.NoContent() : NoRun(id);
        }));

        // A run from the page. 202 with the run, recorded as it goes; 503 without a store (a paid run whose results vanish
        // is the worst outcome); 409 while one runs; its lines as SSE, then "done" with whether it was stored.
        admin.MapPost("/benchmarks/{check}/runs", (string check, StartRunRequest? request) =>
        {
            if (catalog.Checks.FirstOrDefault(c => c.Id == check) is not { } known)
                return ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
                    MessageText.Format(ApiUserText.NoCheck, new { Check = check, Checks = string.Join(", ", catalog.Checks.Select(c => c.Id)) }));
            if (recorder is null)
                return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", MessageText.Format(ApiUserText.NoBenchmarkStore));
            if (runner.IsRunning)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
            if (runner.Start(check, request?.Repeat ?? 1) is not { } run)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.BenchmarkRunning, new { runner.Current!.Check }));
            recorder.Attach(run, settingsOf(known));
            return Results.Json(Row(run), statusCode: StatusCodes.Status202Accepted);
        });

        // The run in progress, or the last one, so a reloaded page can pick it up again.
        admin.MapGet("/benchmarks/runs/current", () => runner.Current is { } run
            ? Results.Json(Row(run))
            : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoBenchmarkRun)));

        admin.MapGet("/benchmarks/runs/{id}/events", (string id, CancellationToken cancellationToken) =>
            runner.Current is { } run && run.Id == id
                ? (IResult)TypedResults.ServerSentEvents(Events(runner, recorder, run, cancellationToken))
                : ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoBenchmarkRun)));
    }

    private static async IAsyncEnumerable<SseItem<string>> Events(BenchmarkRunner runner, BenchmarkRecorder? recorder, BenchmarkRun run,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (string line in runner.EventsAsync(run.Id, cancellationToken))
            yield return new SseItem<string>(line, "line");
        var stored = await StoredAsync(recorder, run.Id, cancellationToken);
        yield return new SseItem<string>(System.Text.Json.JsonSerializer.Serialize(Row(run, stored)), "done");
    }

    /// <summary>A read of the store: 503 without one, or when it doesn't answer, saying why.</summary>
    private static async Task<IResult> FromStore(IBenchmarkStore? store, Func<IBenchmarkStore, Task<IResult>> read)
    {
        if (store is null)
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", MessageText.Format(ApiUserText.NoBenchmarkStore));
        try
        {
            return await read(store);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", Unavailable(ex));
        }
    }

    private static string Unavailable(Exception ex) => MessageText.Format(ApiUserText.BenchmarkStoreUnavailable, new { Reason = ex.Message });

    private static IResult NoRun(string id) =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));

    private static object RunRow(BenchmarkDocuments.Run r) => new
    {
        id = r.Id, check = r.Check, repeat = r.Repeat, status = r.Status.ToString().ToLowerInvariant(), exit_code = r.ExitCode,
        started_at = r.StartedAt, ended_at = r.EndedAt,
        total = r.Total is null ? null : new { cost = r.Total.Cost, currency = r.Total.Currency, tokens = r.Total.Tokens, requests = r.Total.Requests },
        store_error = r.StoreError,
    };

    private static object ConversationRow(BenchmarkDocuments.Conversation c) => new
    {
        mode = c.Key.Mode, repeat = c.Key.Repeat, task = c.Key.Task, order = c.Order, role = c.Role, scores = c.Scores,
        errors = c.Errors, seconds = c.Seconds,
    };

    /// <summary>A turn, with what its tools did, read from its stored events (step 11c's first half): every run has it.</summary>
    private static object TurnRow(BenchmarkDocuments.Turn t, BenchmarkTools.Summary tools) => new
    {
        tools = new { calls = tools.Calls, failed = tools.Failed, reasons = tools.Reasons.Select(r => new { tool = r.Tool, reason = r.Reason, count = r.Count }) },
        mode = t.Key.Mode, repeat = t.Key.Repeat, task = t.Key.Task, turn = t.Key.Turn, session = t.Session, question = t.Question,
        answer = t.Answer, role = t.Role, continued = t.Continued, puls_calls = t.PulsCalls, specialist_calls = t.SpecialistCalls,
        hosts = t.Hosts, seconds = t.Seconds, error = t.Error,
    };

    private static object RequestRow(BenchmarkDocuments.Request r) => new
    {
        turn = r.Turn is null ? null : new { mode = r.Turn.Mode, repeat = r.Turn.Repeat, task = r.Turn.Task, turn = r.Turn.Turn },
        at = r.At, purpose = r.Purpose, session = r.Session, parent = r.Parent, model = r.Model,
        input_tokens = r.InputTokens, output_tokens = r.OutputTokens, cache_read_tokens = r.CacheReadTokens,
        cache_write_tokens = r.CacheWriteTokens, cost = r.Cost, duration_ms = r.DurationMs,
    };

    /// <summary>Whether the run's recording kept everything; false when it failed, took too long or never started.</summary>
    private static async Task<BenchmarkRecorder.Result> StoredAsync(BenchmarkRecorder? recorder, string id, CancellationToken cancellationToken)
    {
        if (recorder?.RecordingOf(id) is not { } recording) return new BenchmarkRecorder.Result(false, null);
        try
        {
            return await recording.WaitAsync(RecordingWait, cancellationToken);
        }
        catch (TimeoutException)
        {
            return new BenchmarkRecorder.Result(false, null);
        }
    }

    private static object Row(BenchmarkRun run, BenchmarkRecorder.Result? stored = null) => new
    {
        id = run.Id, check = run.Check, repeat = run.Repeat, status = run.Status.ToString().ToLowerInvariant(),
        exit_code = run.ExitCode, started_at = run.StartedAt, ended_at = run.EndedAt, lines = run.Lines.Count,
        stored = stored?.Stored, store_error = stored?.StoreError,
    };
}

/// <summary>The body of <c>POST /admin/benchmarks/{check}/runs</c>: how many repeats (1 to the runner's cap).</summary>
internal sealed record StartRunRequest(int? Repeat);
