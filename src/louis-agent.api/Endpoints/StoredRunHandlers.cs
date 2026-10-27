using LouisAgent.Api.Admin.Replies;
using LouisAgent.Api.Benchmarks.Calibrations.Store;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Reviews.Store;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using Microsoft.AspNetCore.Http.HttpResults;
using RunAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Runs.StoredRunReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using EventsAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.Admin.Runs.TurnEventsReply>,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using ProtocolAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.ContentHttpResult,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;
using DeleteAnswer = Microsoft.AspNetCore.Http.HttpResults.Results<
    Microsoft.AspNetCore.Http.HttpResults.NoContent,
    Microsoft.AspNetCore.Http.HttpResults.JsonHttpResult<LouisAgent.Api.Contracts.ApiError>>;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The portal's reads of a stored run, and its delete, as named methods: the run with everything it kept, one turn's
/// stream events, the run as the protocol's own lines, and removing it with its people's grades and judge checks.
/// </summary>
/// <remarks>
/// Introduce Parameter Object (Fowler), with the object owning the behaviour, as <c>AdminHandlers</c>: the routes
/// share the store, the runner and the review and judge-check stores, which aren't services, so they live here and each
/// route is a named method a test can call with no HTTP. Each returns <c>TypedResults</c> with its answers declared.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0039-api-routes-under-v1.md">ADR 0039: the API routes live under /v1</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
internal sealed class StoredRunHandlers(
    IBenchmarkStore? store,
    BenchmarkRunner runner,
    IHumanReviewStore? reviews,
    ICalibrationStore? calibrations)
{
    /// <summary>
    /// A stored run with its conversations, their turns (without their events), its requests, its notes and the
    /// people's grades: the explorer's levels.
    /// </summary>
    public async Task<RunAnswer> GetRun(string id, CancellationToken cancellationToken)
    {
        if (store is null) return NoStore();
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            return TypedResults.Json(await ReadAsync(store, run, cancellationToken), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>One turn's stream events, for its replay: the heaviest read, so only on demand.</summary>
    public async Task<EventsAnswer> GetTurnEvents(string id, string mode, int repeat, string task, int turn,
        CancellationToken cancellationToken)
    {
        if (store is null) return NoStore();
        try
        {
            var events = await store.TurnEventsAsync(id, new BenchmarkProtocol.TurnKey(mode, repeat, task, turn),
                cancellationToken);
            return TypedResults.Json(new TurnEventsReply([.. events.Select(TurnEventRow.Of)]), AdminJson.SnakeCase);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>
    /// A stored run as the protocol's own lines (F18 step 5.2): what a recorded run hands back to the harness as a
    /// transcript.
    /// </summary>
    public async Task<ProtocolAnswer> GetProtocol(string id, CancellationToken cancellationToken)
    {
        if (store is null) return NoStore();
        try
        {
            if (await store.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            string lines = await BenchmarkProtocolExport.BuildAsync(store, run, cancellationToken);
            return TypedResults.Text(lines, "text/plain; charset=utf-8");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>
    /// Deletes a stored run with the people's grades and the judge checks of it; refused while that run is running.
    /// </summary>
    public async Task<DeleteAnswer> DeleteRun(string id, CancellationToken cancellationToken)
    {
        if (store is null) return NoStore();
        if (runner.Current is { Status: BenchmarkRunStatus.Running } live && live.Id == id)
            return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error",
                MessageText.Format(ApiUserText.StoredRunStillRunning, new { Id = id }));
        try
        {
            if (!await store.DeleteRunAsync(id, cancellationToken)) return NoRun(id);
            if (reviews is not null) await reviews.DeleteRunAsync(id, cancellationToken);
            if (calibrations is not null) await calibrations.DeleteRunAsync(id, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return Unavailable(ex);
        }
    }

    /// <summary>Reads everything the run kept, and the people's grades of it, into the explorer's reply.</summary>
    private async Task<StoredRunReply> ReadAsync(IBenchmarkStore s, BenchmarkDocuments.Run run,
        CancellationToken cancellationToken)
    {
        var conversations = await s.ConversationsAsync(run.Id, cancellationToken);
        var turns = new List<TurnRow>();
        foreach (var conversation in conversations)
            foreach (var turn in await s.TurnsAsync(run.Id, conversation.Key, cancellationToken))
                turns.Add(TurnRow.Of(turn, await BenchmarkTools.OfTurnAsync(s, run.Id, turn, cancellationToken)));
        var requests = await s.RequestsAsync(run.Id, cancellationToken);
        var notes = await s.NotesAsync(run.Id, cancellationToken);

        // The people's grades: each reviewer's latest beside its conversation, and the decided ones for agreement.
        var people = reviews is null ? [] : await reviews.OfRunAsync(run.Id, cancellationToken);
        var latest = HumanGrades.Latest(people);
        var decided = HumanGrades.Decided(people);
        var agreement = ReviewQueries.HumanReviewsOf(run)
            .Select(human => human[..^HumanReview.Suffix.Length])
            .Order(StringComparer.Ordinal)
            .Select(grader => AgreementRow.Of(grader, conversations, decided))
            .ToList();

        return new StoredRunReply(
            StoredRunRow.Of(run),
            run.Settings,
            [.. (run.Modes ?? []).Select(ModeRow.Of)],
            run.Log,
            [.. notes.Select(NoteRow.Of)],
            SummaryRow.Of(run.Slices, run.Total, requests),
            [.. conversations.Select(c => ConversationRow.Of(c, [.. latest.Where(g => g.Conversation == c.Key)]))],
            agreement,
            turns,
            [.. requests.Select(RequestRow.Of)]);
    }

    /// <summary>The 503 for a store that isn't there.</summary>
    private static JsonHttpResult<ApiError> NoStore() =>
        ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error",
            MessageText.Format(ApiUserText.NoBenchmarkStore));

    /// <summary>The 503 for a store that didn't answer, saying why.</summary>
    private static JsonHttpResult<ApiError> Unavailable(Exception ex) =>
        ApiErrors.Result(StatusCodes.Status503ServiceUnavailable, "api_error", AdminEndpoints.Unavailable(ex));

    /// <summary>The 404 for a run that isn't stored.</summary>
    private static JsonHttpResult<ApiError> NoRun(string id) =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error",
            MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));
}
