using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Endpoints;
using LouisAgent.Api.Evals.Messages;
using LouisAgent.Api.Evals.Messages.Holes;
using LouisAgent.Api.Evals.Messages.Holes.Runs;
using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Contracts.Api;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Json;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Runs.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using FoundRun = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Evals.Contracts.Runs.EvalRun>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using FoundRuns = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<System.Collections.Generic.IReadOnlyList<
        LouisAgent.Evals.Contracts.Api.RunRow>>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using StopAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.NoContent,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using StartAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.Accepted,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Evals;

/// <summary>
/// The four eval routes' answers: the host's evals, a run started, a run read at its own address, and an eval's
/// runs. A start is refused with its reason before anything runs. What is sent is what is kept, in the run's own
/// JSON.
/// </summary>
/// <remarks>
/// Named handlers returning typed results, their checks first as guard clauses (ADR 0051). A start answers 202 with
/// the run's address, Microsoft's Asynchronous Request-Reply, except that the address keeps answering the run once
/// it has ended, since running and ended are the same record.
/// </remarks>
/// <param name="catalog">The host's evals.</param>
/// <param name="desk">The one run going.</param>
/// <param name="work">How a run is played and kept, or none on a host with no data folder.</param>
/// <param name="keepsRecordings">Whether the host keeps recordings, which every live run makes.</param>
/// <param name="clock">The time a run starts at.</param>
/// <seealso href="https://learn.microsoft.com/azure/architecture/patterns/async-request-reply">Asynchronous Request-Reply pattern</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0051-flat-conditionals-and-named-endpoints.md">ADR 0051: flat conditionals and named endpoints</seealso>
public sealed class EvalHandlers(
    EvalCatalog catalog,
    RunDesk desk,
    RunWork? work,
    bool keepsRecordings,
    TimeProvider clock)
{
    /// <summary>Where a run is read, by its id.</summary>
    public const string RunAddress = "/v1/admin/evals/runs/";

    /// <summary>Every eval this host has, with its last run in short.</summary>
    public JsonHttpResult<EvalsReply> List()
    {
        var rows = catalog.Names.Select(name => Row(name, catalog.Find(name)!)).ToList();
        return TypedResults.Json(new EvalsReply(rows), RunJson.Options);
    }

    /// <summary>Starts a run of the eval, once every reason to refuse it has been asked.</summary>
    public async Task<StartAnswer> StartAsync(string name, HttpRequest body)
    {
        if (catalog.Find(name) is not { } file) return Unknown(name);
        if (await RequestOf(body) is not { } request)
            return ApiErrors.BadRequest(MessageText.Format(EvalsApiMessage.StartNotRead));
        if (request.Repeats is not (1 or 3))
        {
            var repeats = new RepeatsNotOneOrThreeHoles(request.Repeats);
            return ApiErrors.BadRequest(MessageText.Format(EvalsApiMessage.RepeatsNotOneOrThree, repeats));
        }

        if (work is null)
        {
            var setting = new NowhereToKeepHoles(EvalsOptions.DataFolderSetting);
            return ApiErrors.BadRequest(MessageText.Format(EvalsApiMessage.NowhereToKeep, setting));
        }

        if (request.Kind == RunKind.Live && !keepsRecordings)
            return ApiErrors.BadRequest(MessageText.Format(EvalsApiMessage.LiveNeedsRecordings));

        var replayed = request.Kind == RunKind.Replay ? LastLiveRun(work.Store, name) : null;
        if (request.Kind == RunKind.Replay && replayed is null)
            return ApiErrors.BadRequest(MessageText.Format(EvalsApiMessage.NoLiveRunToReplay));

        var started = Started(name, file, request, replayed, work.Store);
        if (!desk.TryStart(started, work.For(started, file)))
        {
            var going = new RunGoingHoles(desk.Current?.Id ?? "");
            return ApiErrors.Conflict(MessageText.Format(EvalsApiMessage.RunGoing, going));
        }

        // The address is the answer: the run is read there in its own JSON, as it goes and once it has ended.
        return TypedResults.Accepted(RunAddress + started.Id);
    }

    /// <summary>The run with this id, as it is kept, running or ended.</summary>
    public FoundRun Find(string id)
    {
        if (!RunId.TryParse(id, out _)) return NoSuchRun(id);
        // Kept, or the run going here before its first save.
        if ((work?.Store.Find(id) ?? GoingAs(id)) is not { } run) return NoSuchRun(id);

        return TypedResults.Json(desk.Read(run), RunJson.Options);
    }

    /// <summary>The eval's runs in short, the newest first.</summary>
    /// <summary>
    /// <c>DELETE /v1/admin/evals/runs/{id}</c>: stops the run going here, which then keeps itself as stopped. A run
    /// this host doesn't have is 404, and one that isn't going 409, with the reason.
    /// </summary>
    public StopAnswer Stop(string id)
    {
        if (!RunId.TryParse(id, out _)) return NoSuchRun(id);
        if ((work?.Store.Find(id) ?? GoingAs(id)) is null) return NoSuchRun(id);
        if (desk.TryStop(id)) return TypedResults.NoContent();

        var notGoing = new RunNotGoingHoles(id);
        return ApiErrors.Conflict(MessageText.Format(EvalsApiMessage.RunNotGoing, notGoing));
    }

    public FoundRuns RunsOf(string name)
    {
        if (catalog.Find(name) is null) return Unknown(name);

        IReadOnlyList<RunRow> rows = RunsIn(name).Select(RunRow.Of).ToList();
        return TypedResults.Json(rows, RunJson.Options);
    }

    /// <summary>The start's body, read with the run's own options so its kind is read by its wire name.</summary>
    private static async Task<StartRunRequest?> RequestOf(HttpRequest body)
    {
        try
        {
            return await body.ReadFromJsonAsync<StartRunRequest>(RunJson.Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>The run going here, when it has this id.</summary>
    private EvalRun? GoingAs(string id) => desk.Current?.Id == id ? desk.Current : null;

    /// <summary>An eval's row, with its newest run and its newest live run that finished.</summary>
    private EvalRow Row(string name, EvalFile file)
    {
        var last = RunsIn(name).FirstOrDefault();
        var lastLive = work is null ? null : LastLiveRun(work.Store, name);
        return new EvalRow(name, file.Agent.ToString(), file.Conversations.Count,
            last is null ? null : RunRow.Of(last), lastLive is null ? null : RunRow.Of(lastLive));
    }

    /// <summary>The eval's runs as they really are, the newest first; none without a data folder.</summary>
    private IEnumerable<EvalRun> RunsIn(string name) => (work?.Store.Of(name) ?? []).Select(desk.Read);

    /// <summary>The run as it starts: its id from the time to the second and the eval's name.</summary>
    private EvalRun Started(string name, EvalFile file, StartRunRequest request, EvalRun? replayed, RunStore store)
    {
        var now = clock.GetUtcNow();
        var at = new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, TimeSpan.Zero);
        // An id names one run: a start in a second another run started in takes the next second.
        while (store.Find(new RunId(at, name).Text) is not null) at = at.AddSeconds(1);

        // A replay plays the live run's repeats back, as many as it recorded.
        int repeats = replayed?.Repeats ?? request.Repeats;
        return new EvalRun(new RunId(at, name).Text, name, file.Agent.ToString(), request.Kind, repeats, at, [])
        {
            ReplayOf = replayed?.Id
        };
    }

    /// <summary>The eval's newest live run that finished, the one a replay plays back, or none.</summary>
    private EvalRun? LastLiveRun(RunStore store, string name) => store.Of(name).Select(desk.Read)
        .FirstOrDefault(run => run is { Kind: RunKind.Live, Status: RunStatus.Finished });

    /// <summary>The refusal of an eval this host doesn't have.</summary>
    private static JsonHttpResult<ApiError> Unknown(string name)
    {
        var unknown = new UnknownEvalHoles(name);
        return ApiErrors.NotFound(MessageText.Format(EvalsApiMessage.UnknownEval, unknown));
    }

    /// <summary>The answer for an id that names no run.</summary>
    private static JsonHttpResult<ApiError> NoSuchRun(string id)
    {
        var none = new NoSuchRunHoles(id);
        return ApiErrors.NotFound(MessageText.Format(EvalsApiMessage.NoSuchRun, none));
    }
}
