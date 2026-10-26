using LouisAgent.Api.Admin;
using LouisAgent.Api.Benchmarks.Export;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Reviews;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using static LouisAgent.Api.Admin.AdminRows;

namespace LouisAgent.Api.Endpoints;

/// <summary>
/// The admin portal's reads and deletes of a stored benchmark run (F31, F18 step 5.2): the run with its conversations, turns and requests,
/// one turn's stream events, the run as the protocol's own lines, and its deletion. They belong to the same <c>/admin</c> group as the rest,
/// so <see cref="AdminEndpoints"/> maps them and the group's key gate covers them.
/// </summary>
internal static class StoredRunEndpoints
{
    public static void MapStoredRuns(this RouteGroupBuilder admin, IBenchmarkStore? store, BenchmarkRunner runner, IHumanReviewStore? reviews = null)
    {
        // A stored run with its conversations, their turns (without their events) and its requests: the explorer's levels.
        admin.MapGet("/benchmarks/runs/{id}", (string id, CancellationToken cancellationToken) => AdminEndpoints.FromStore(store, async s =>
        {
            if (await s.GetRunAsync(id, cancellationToken) is not { } run) return NoRun(id);
            var conversations = await s.ConversationsAsync(id, cancellationToken);
            var turns = new List<object>();
            foreach (var conversation in conversations)
                foreach (var turn in await s.TurnsAsync(id, conversation.Key, cancellationToken))
                    turns.Add(TurnRow(turn, await BenchmarkTools.OfTurnAsync(s, id, turn, cancellationToken)));
            var requests = await s.RequestsAsync(id, cancellationToken);
            var people = reviews is null ? [] : await reviews.OfRunAsync(id, cancellationToken);
            var latest = HumanGrades.Latest(people);
            var decided = HumanGrades.Decided(people);
            return Results.Json(new
            {
                run = RunRow(run),
                settings = run.Settings,
                modes = (run.Modes ?? []).Select(m => new
                {
                    name = m.Name, repeat = m.Repeat, toolsets = m.Toolsets, role = m.Role, llm = m.Llm,
                    // The runner's rates for the repeats up to this one (F18 step 5a): null on a mode from before them, never 0.
                    pass_at_k = m.Rates?.PassAtK, pass_hat_k = m.Rates?.PassHatK, k = m.Rates?.K, infrastructure = m.Rates?.Infrastructure, retried = m.Rates?.Retried,
                }),
                log = run.Log,
                // F18 step 5c.2: each mode's roll-up as the runner wrote it, priced from the ledger; null for a run from before it.
                summaries = SummaryRow.Of(run.Slices, run.Total, requests),
                conversations = conversations.Select(c => ConversationRow(c, [.. latest.Where(g => g.Conversation == c.Key)])),
                // F18 step 6a.2: per judge with a human review, how often it agreed with the people, as counts (the API reads, it doesn't rate).
                agreement = ReviewEndpoints.HumanReviewsOf(run).Select(human => human[..^".human".Length]).Order(StringComparer.Ordinal).Select(grader =>
                {
                    var t = ReviewAgreement.Of(conversations, decided, grader);
                    return new
                    {
                        grader,
                        person_pass = new { judge_pass = t.PersonPassJudgePass, judge_fail = t.PersonPassJudgeFail, judge_unknown = t.PersonPassJudgeUnknown },
                        person_fail = new { judge_pass = t.PersonFailJudgePass, judge_fail = t.PersonFailJudgeFail, judge_unknown = t.PersonFailJudgeUnknown },
                    };
                }),
                turns,
                requests = requests.Select(RequestRow),
            });
        }));

        // One turn's stream events, for its replay: the heaviest read, so only on demand.
        admin.MapGet("/benchmarks/runs/{id}/turns/{mode}/{repeat:int}/{task}/{turn:int}/events",
            (string id, string mode, int repeat, string task, int turn, CancellationToken cancellationToken) => AdminEndpoints.FromStore(store, async s =>
            {
                var events = await s.TurnEventsAsync(id, new BenchmarkProtocol.TurnKey(mode, repeat, task, turn), cancellationToken);
                return Results.Json(new { events = events.Select(e => new { type = e.Type, data = e.Data, at = e.At }) });
            }));

        // A stored run as the protocol's own lines (F18 step 5.2): what a recorded run hands back to the harness as a transcript.
        admin.MapGet("/benchmarks/runs/{id}/protocol", (string id, CancellationToken cancellationToken) => AdminEndpoints.FromStore(store, async s =>
            await s.GetRunAsync(id, cancellationToken) is { } run
                ? Results.Text(await BenchmarkProtocolExport.BuildAsync(s, run, cancellationToken), "text/plain; charset=utf-8")
                : NoRun(id)));

        admin.MapDelete("/benchmarks/runs/{id}", (string id, CancellationToken cancellationToken) => AdminEndpoints.FromStore(store, async s =>
        {
            if (runner.Current is { Status: BenchmarkRunStatus.Running } live && live.Id == id)
                return ApiErrors.Result(StatusCodes.Status409Conflict, "conflict_error", MessageText.Format(ApiUserText.StoredRunStillRunning, new { Id = id }));
            if (!await s.DeleteRunAsync(id, cancellationToken)) return NoRun(id);
            if (reviews is not null) await reviews.DeleteRunAsync(id, cancellationToken);
            return Results.NoContent();
        }));
    }

    private static IResult NoRun(string id) =>
        ApiErrors.Result(StatusCodes.Status404NotFound, "not_found_error", MessageText.Format(ApiUserText.NoStoredRun, new { Id = id }));
}
