using LouisAgent.Choices;
using LouisAgent.Api.Benchmarks.Calibrations;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;

namespace LouisAgent.Api.Admin;

/// <summary>
/// The JSON rows the admin endpoints send (F27, F31): a stored run, a conversation, a turn with what its tools did, a ledger request,
/// and a live run. Each is an anonymous object with the portal's snake_case names, written by <c>Results.Json</c>. The standard records
/// (F18 step 4.5b) ride on the run (its eval, version, commit and graders) and on each conversation (its grades and outcome), with
/// their enums spelt as the protocol and the store spell them (<c>FixedRule</c>, <c>Pass</c>), as <c>models[].level</c> is, so one
/// spelling runs from the runner to the portal. A run from before step 2a sends null for them, never <c>[]</c> or 0 (ADR 0007).
/// </summary>
internal static class AdminRows
{
    public static object RunRow(BenchmarkDocuments.Run r) => new
    {
        id = r.Id, check = r.Check, repeat = r.Repeat, status = r.Status.Wire(), exit_code = r.ExitCode,
        started_at = r.StartedAt, ended_at = r.EndedAt,
        total = r.Total is null ? null : new { cost = r.Total.Cost, currency = r.Total.Currency, tokens = r.Total.Tokens, requests = r.Total.Requests },
        store_error = r.StoreError,
        models = r.Models?.Select(m => new { m.Mode, level = m.Level.ToString(), m.Role, m.Model }),
        eval = r.Eval, eval_version = r.EvalVersion, commit = r.Commit, rescore_of = r.RescoreOf,
        graders = r.Graders?.Select(g => new { name = g.Name, version = g.Version, kind = g.Kind.ToString(), question = g.Question }),
    };

    public static object ConversationRow(BenchmarkDocuments.Conversation c, IReadOnlyList<Benchmarks.Reviews.HumanGrade>? human = null) => new
    {
        mode = c.Key.Mode, repeat = c.Key.Repeat, task = c.Key.Task, order = c.Order, role = c.Role, scores = c.Scores,
        errors = c.Errors, seconds = c.Seconds,
        grades = c.Grades?.Select(g => new { grader = g.Grader, version = g.Version, score = g.Score, outcome = g.Outcome.ToString(), reason = g.Reason, judge_model = g.JudgeModel, judge_prompt_hash = g.JudgePromptHash }),
        outcome = c.Outcome?.ToString(),
        // People's grades of this conversation (F18 step 6a.2): each reviewer's latest, beside the grades above; empty when nobody has graded it.
        human_grades = (human ?? []).Select(g => Endpoints.ReviewEndpoints.Row(g, latest: true)),
    };

    /// <summary>A turn, with what its tools did, read from its stored events (step 11c's first half): every run has it.</summary>
    public static object TurnRow(BenchmarkDocuments.Turn t, BenchmarkTools.Summary tools) => new
    {
        tools = new { calls = tools.Calls, failed = tools.Failed, reasons = tools.Reasons.Select(r => new { tool = r.Tool, reason = r.Reason, count = r.Count }) },
        mode = t.Key.Mode, repeat = t.Key.Repeat, task = t.Key.Task, turn = t.Key.Turn, session = t.Session, question = t.Question,
        answer = t.Answer, role = t.Role, continued = t.Continued, puls_calls = t.PulsCalls, specialist_calls = t.SpecialistCalls,
        hosts = t.Hosts, seconds = t.Seconds, error = t.Error,
    };

    public static object RequestRow(BenchmarkDocuments.Request r) => new
    {
        turn = r.Turn is null ? null : new { mode = r.Turn.Mode, repeat = r.Turn.Repeat, task = r.Turn.Task, turn = r.Turn.Turn },
        at = r.At, purpose = r.Purpose, session = r.Session, parent = r.Parent, model = r.Model,
        input_tokens = r.InputTokens, output_tokens = r.OutputTokens, cache_read_tokens = r.CacheReadTokens,
        cache_write_tokens = r.CacheWriteTokens, cost = r.Cost, duration_ms = r.DurationMs,
    };

    /// <summary>
    /// A live run: the runner's view, and whether its recording kept everything once it is done. A judge check (F18 step 6a.5b) says which run and judge it checks, and its last line,
    /// since its lines are not streamed to a page: the runner's refusal, when it refused, is the last.
    /// </summary>
    public static object Row(BenchmarkRun run, BenchmarkRecorder.Result? stored = null) => new
    {
        id = run.Id, check = run.Check, repeat = run.Repeat, status = run.Status.Wire(),
        exit_code = run.ExitCode, started_at = run.StartedAt, ended_at = run.EndedAt, lines = run.Lines.Count,
        stored = stored?.Stored, store_error = stored?.StoreError,
        calibration_of = run.Calibration?.Of, grader = run.Calibration?.Grader,
        last_line = run.Calibration is null ? null : run.Lines.LastOrDefault(l => !string.IsNullOrWhiteSpace(l)),
    };

    /// <summary>A judge check's report (F18 step 6a.5b), in the agreement card's shape: each class as the judge's pass, fail and unknown counts.</summary>
    public static object CalibrationRow(CalibrationRecord c) => new
    {
        run = c.Run, grader = c.Grader, judge_model = c.JudgeModel, at = c.At, agreed = c.Agreed, total = c.Total, bar = c.Bar, passes = c.Passes,
        person_pass = new { judge_pass = c.PersonPass.JudgePass, judge_fail = c.PersonPass.JudgeFail, judge_unknown = c.PersonPass.JudgeUnknown },
        person_fail = new { judge_pass = c.PersonFail.JudgePass, judge_fail = c.PersonFail.JudgeFail, judge_unknown = c.PersonFail.JudgeUnknown },
        disagreements = c.Disagreements.Select(d => new { scenario = d.Scenario, mode = d.Mode, repeat = d.Repeat, label = d.Label, judge = d.Judge, reason = d.Reason }),
    };
}
