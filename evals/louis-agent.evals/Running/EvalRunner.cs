using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.Logging;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Running.Rescoring;
using LouisAgent.Evals.Protocol.Parts.Run;
using ModelLevel = LouisAgent.Evals.Protocol.Kinds.ModelLevel;

namespace LouisAgent.Evals.Running;

/// <summary>
/// Runs an eval (F18 step 2): each mode, each repeat, each scenario the run's split asks for (step 5.4); each message
/// is sent to the API and the session's events are written as they arrive, then the turn, and the conversation with its
/// grades (every grader of the eval, <see cref="TrialGrading"/>), its outcome (<see cref="PassRates.Of"/>) and its
/// scores, then the mode's end (F18 step 4.5a).
/// </summary>
public sealed class EvalRunner(
    ISessionClient sessions,
    ProtocolWriter protocol,
    TimeProvider clock,
    ILogger<EvalRunner> logger)
{
    /// <summary>
    /// The go at one scenario: its session, its turns and its close. The runner decides whether to go again.
    /// </summary>
    private readonly ScenarioAttempt _attempt = new(sessions, protocol, clock, logger);

    /// <summary>Runs every mode of the eval, repeat after repeat, and writes the protocol as it goes.</summary>
    public async Task RunAsync(IEval eval, EvalRun run, CancellationToken cancellationToken)
    {
        var modes = await eval.ModesAsync(sessions, cancellationToken);
        var scenarios = eval.Scenarios.Where(scenario => run.Split.Includes(scenario.Split)).ToList();
        logger.Log(LogLevel.Information, EvalLogMessage.RunStarted,
            new
            {
                Eval = eval.Name,
                Modes = modes.Count,
                Repeats = run.Repeats,
                Run = run.RunId
            });
        // The model isn't reported yet: the session client that could read it is the HTTP client, which is step 5.
        protocol.Begin(eval.Name, eval.Version, EvalBuild.Commit, eval.Graders,
            modes.Select(m => new ModelInfo(m.Name, ModelLevel.Agent, m.Role, null)).ToList(), run.Split);

        foreach (var mode in modes)
        {
            var tally = new ModeTally();
            for (int repeat = 1; repeat <= run.Repeats; repeat++)
            {
                protocol.Mode(mode.Name, repeat, mode.Toolsets, mode.Role, mode.Llm, scenarios.Count);
                foreach (var scenario in scenarios)
                    await RunScenarioAsync(eval, run, mode, repeat, scenario, tally, cancellationToken);
                // The rates over the repeats so far: the last line of a mode is the mode's (F18 step 5a).
                protocol.ModeDone(mode.Name, repeat, tally.Share, repeat, tally.Infrastructure, tally.Retried);
            }

            // The mode rolled up, overall and by category (F18 step 5c), after its last mode_done.
            foreach (var line in RunSummary.Of(mode.Name, tally.Results, eval.Graders)) protocol.Summary(line);
        }
    }

    /// <summary>
    /// Grades a stored run's transcripts again, with today's graders, and records the result as a new run that names
    /// the one it re-scored (<c>rescore_of</c>). No session is opened, no model is called and no ledger row is written;
    /// the stored lines are only read. A grader change shouldn't cost a paid run: this is "evals as regression tests
    /// over recorded behaviour", the transcripts the fixed input and the graders the code under test (GOAL skills 12
    /// and 13). The plan's signature took a <c>StoredRun</c> the API would build; the runner reads the protocol's own
    /// lines instead (the same reader a run and the conformance check use), so the API only pipes its export.
    /// </summary>
    public Task RescoreAsync(IEval eval, string runId, string rescoreOf, IEnumerable<string> storedLines,
        CancellationToken cancellationToken) =>
        new Rescorer(protocol, logger).RunAsync(eval, runId, rescoreOf, storedLines, cancellationToken);

    /// <summary>
    /// One scenario of a mode's repeat: the attempt, one retry for a transport failure, then the grades, the scores and
    /// the conversation's line.
    /// </summary>
    private async Task RunScenarioAsync(IEval eval, EvalRun run, EvalMode mode, int repeat, EvalScenario scenario,
        ModeTally tally,
        CancellationToken cancellationToken)
    {
        if (scenario.Input.Messages.Count == 0)
        {
            logger.Log(LogLevel.Warning, EvalLogMessage.NoMessages, new { Scenario = scenario.Id });
            return;
        }

        var started = clock.GetUtcNow();
        var (turns, facts) = await _attempt.RunAsync(eval, run, mode, repeat, scenario, cancellationToken);
        // One retry, for a transport failure only: a timeout, an API error or a replay miss would not change on a
        // second try (F18 step 5a). The second attempt is a new session and the trial's; the failed one stays in the
        // protocol, and the same turn keys written again replace it in the store.
        if (turns.FirstOrDefault(turn => turn.Failure is not null) is { Failure: TurnFailure.Transport } failed)
        {
            logger.Log(LogLevel.Warning, EvalLogMessage.TrialRetried,
                new
                {
                    Scenario = scenario.Id,
                    Turn = failed.Turn
                });
            tally.CountRetry();
            (turns, facts) = await _attempt.RunAsync(eval, run, mode, repeat, scenario, cancellationToken);
        }

        var trial = new Trial(run.RunId, mode, repeat, scenario, new Transcript(turns));
        var grades = await TrialGrading.GradeAsync(eval, trial, logger, cancellationToken);
        var outcome = PassRates.Of(trial, grades, eval.Graders);
        // The eval's own record, serialized as its runtime type: as EvalScores, which has no properties, it would be
        // {}.
        var scores = JsonSerializer.SerializeToElement<object>(eval.ScoresOf(trial, grades), EvalScores.Json);
        tally.Add(new TrialResult(scenario.Id, scenario.Categories, outcome, grades, scores));
        // The conversation's role is the one that answered its first turn; its errors are the turns that failed.
        protocol.Conversation(mode.Name, repeat, scenario.Id, facts[0].Role, scores,
            (clock.GetUtcNow() - started).TotalSeconds, grades, outcome, errors: facts.Count(f => f.Error is not null));
    }
}
