using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Reports;
using LouisAgent.Evals.Trials;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Evals.Running.Rescoring;

/// <summary>
/// Grades a stored run again with today's graders, opening no session. The first line is written again with today's
/// eval, graders and dataset, and the run it re-scored; the mode, question, event and turn lines are copied as they
/// were; each conversation line is replaced by a new one, graded from the transcript its own lines make; each mode's
/// end is replaced by the rates of those new grades.
/// </summary>
internal sealed class Rescorer(ProtocolWriter protocol, ILogger logger)
{
    /// <summary>Walks the stored run's lines in order and writes the new run's lines as it goes.</summary>
    /// <remarks>
    /// A switch on the event's type, one case per kind of line, read like a table. The first line and the summaries
    /// fall through: the new run writes its own.
    /// </remarks>
    public async Task RunAsync(IEval eval, string runId, string rescoreOf, IEnumerable<string> storedLines,
        CancellationToken cancellationToken)
    {
        var stored = StoredRun.Read(storedLines);
        logger.Log(LogLevel.Information, EvalLogMessage.RescoreStarted,
            new
            {
                Source = rescoreOf,
                Run = runId,
                Conversations = stored.Lines.Count(line => line.Event is ConversationEvent)
            });
        protocol.Begin(eval.Name, eval.Version, EvalBuild.Commit, eval.Checks, stored.Models, stored.Split, rescoreOf,
            eval.Dataset);

        string? current = null;
        var modes = new Dictionary<string, EvalMode>(StringComparer.Ordinal);
        var tallies = new Dictionary<string, ModeTally>(StringComparer.Ordinal);
        var turns = new Dictionary<ConversationKey, List<EvalEvent>>();
        foreach (var line in stored.Lines)
        {
            switch (line.Event)
            {
                case ModeEvent mode:
                    // A mode's repeats are consecutive: when the next mode begins, the last one is done (F18 step 5c).
                    if (current is not null && current != mode.Mode) WriteSummary(eval, current, tallies[current]);
                    current = mode.Mode;
                    modes[mode.Mode] = StoredRun.ModeOf(mode);
                    tallies.TryAdd(mode.Mode, new ModeTally());
                    protocol.Copy(line.Raw);
                    break;
                case QuestionEvent or SseEvent or TurnEvent:
                    protocol.Copy(line.Raw);
                    var key = StoredRun.ConversationOf(line.Event)!;
                    if (!turns.TryGetValue(key, out var events)) turns[key] = events = [];
                    events.Add(line.Event);
                    break;
                case ConversationEvent conversation:
                    turns.TryGetValue(conversation.Key, out var own);
                    await RescoreConversationAsync(eval, runId, modes[conversation.Mode], conversation, own ?? [],
                        tallies[conversation.Mode], cancellationToken);
                    break;
                case ModeDoneEvent done:
                    var tally = tallies[done.Mode];
                    tally.CarryRetried(done.Rates?.Retried ?? 0);
                    protocol.ModeDone(done.Mode, done.Repeat, tally.Share, done.Repeat, tally.Infrastructure,
                        tally.Retried);
                    break;
            }
        }

        if (current is not null) WriteSummary(eval, current, tallies[current]);
    }

    /// <summary>
    /// The mode rolled up from its new grades; the stored run's own <c>summary</c> lines are never copied, since they
    /// are of the old grades.
    /// </summary>
    private void WriteSummary(IEval eval, string mode, ModeTally tally)
    {
        foreach (var line in RunSummary.Of(mode, tally.Results, eval.Checks)) protocol.Summary(line);
    }

    /// <summary>
    /// Grades one stored conversation again from its own turn lines, and writes its new conversation line.
    /// </summary>
    private async Task RescoreConversationAsync(IEval eval, string runId, EvalMode mode, ConversationEvent stored,
        List<EvalEvent> turnEvents, ModeTally tally,
        CancellationToken cancellationToken)
    {
        var transcript = ProtocolTranscripts.Of(turnEvents);
        var scenario = eval.Scenarios.FirstOrDefault(s => s.Id == stored.Task);
        // A scenario renamed or removed since the run is Unknown, never a Fail: the agent didn't do worse, the
        // question is gone.
        bool gone = scenario is null;
        scenario ??= new EvalScenario(stored.Task, new EvalInput(null, string.Empty, []), default, [],
            DatasetSplit.Dev);
        var trial = new Trial(runId, mode, stored.Repeat, scenario, transcript);

        IReadOnlyList<Grade> grades;
        if (gone)
        {
            logger.Log(LogLevel.Warning, EvalLogMessage.ScenarioGone, new { Scenario = stored.Task });
            string reason = MessageText.Format(CommonGradeMessage.ScenarioGone, new { Scenario = stored.Task });
            grades = [.. eval.Checks.Select(grader => Verdict.Unknown(grader.Name, grader.Version, reason))];
        }
        else
        {
            grades = await TrialChecking.GradeAsync(eval, trial, logger, cancellationToken);
        }

        var outcome = PassRates.Of(trial, grades, eval.Checks);
        tally.Add(new TrialResult(stored.Task, gone ? [] : scenario.Categories, outcome, grades));

        // Measured again from the stored turns, the way the run measured them, so the metrics are the run's.
        var facts = transcript.Turns.Select(turn => eval.TurnFactsOf(turn, mode)).ToList();
        protocol.Conversation(stored.Mode, stored.Repeat, stored.Task, stored.Role,
            ConversationMetrics.Of(facts, stored.Seconds), stored.Seconds, grades, outcome, stored.Errors);
    }
}
