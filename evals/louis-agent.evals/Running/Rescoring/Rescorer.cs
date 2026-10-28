using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Outcomes;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol;
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
    /// <remarks>A switch on the line's kind, one case per kind of line, read like a table.</remarks>
    public async Task RunAsync(IEval eval, string runId, string rescoreOf, IEnumerable<string> storedLines,
        CancellationToken cancellationToken)
    {
        var stored = StoredRun.Read(storedLines);
        logger.Log(LogLevel.Information, EvalLogMessage.RescoreStarted,
            new
            {
                Source = rescoreOf,
                Run = runId,
                Conversations = stored.Lines.Count(line => line.Kind == "conversation")
            });
        protocol.Begin(eval.Name, eval.Version, EvalBuild.Commit, eval.Graders, stored.Models, stored.Split, rescoreOf,
            eval.Dataset);

        string? current = null;
        var modes = new Dictionary<string, EvalMode>(StringComparer.Ordinal);
        var tallies = new Dictionary<string, ModeTally>(StringComparer.Ordinal);
        var turns = new Dictionary<(string Mode, int Repeat, string Task), List<string>>();
        foreach (var line in stored.Lines)
        {
            switch (line.Kind)
            {
                case "mode":
                    // A mode's repeats are consecutive: when the next mode begins, the last one is done (F18 step 5c).
                    if (current is not null && current != line.Mode) WriteSummary(eval, current, tallies[current]);
                    current = line.Mode;
                    modes[line.Mode] = StoredRun.ModeOf(line);
                    tallies.TryAdd(line.Mode, new ModeTally());
                    protocol.Copy(line.Raw);
                    break;
                case "question" or "sse" or "turn":
                    protocol.Copy(line.Raw);
                    if (!turns.TryGetValue(line.Key, out var lines)) turns[line.Key] = lines = [];
                    lines.Add(line.Raw);
                    break;
                case "conversation":
                    turns.TryGetValue(line.Key, out var own);
                    await RescoreConversationAsync(eval, runId, modes[line.Mode], line, own ?? [], tallies[line.Mode],
                        cancellationToken);
                    break;
                // A mode's end, in version 1's word or version 2's, until step 7c.2e reads the records.
                case "mode_done" or "mode-done":
                    var tally = tallies[line.Mode];
                    tally.CarryRetried(line.Retried);
                    protocol.ModeDone(line.Mode, line.Repeat, tally.Share, line.Repeat, tally.Infrastructure,
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
        foreach (var line in RunSummary.Of(mode, tally.Results, eval.Graders)) protocol.Summary(line);
    }

    /// <summary>
    /// Grades one stored conversation again from its own turn lines, and writes its new conversation line.
    /// </summary>
    private async Task RescoreConversationAsync(IEval eval, string runId, EvalMode mode, StoredLine stored,
        List<string> turnLines, ModeTally tally,
        CancellationToken cancellationToken)
    {
        var transcript = ProtocolTranscripts.Read(turnLines, stored.Mode, stored.Repeat, stored.Task);
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
            grades = [.. eval.Graders.Select(grader => Verdict.Unknown(grader.Name, grader.Version, reason))];
        }
        else
        {
            grades = await TrialGrading.GradeAsync(eval, trial, logger, cancellationToken);
        }

        var outcome = PassRates.Of(trial, grades, eval.Graders);
        tally.Add(new TrialResult(stored.Task, gone ? [] : scenario.Categories, outcome, grades));

        // Measured again from the stored turns, the way the run measured them, so the metrics are the run's.
        double seconds = stored.Decimal("seconds") ?? 0;
        var facts = transcript.Turns.Select(turn => eval.TurnFactsOf(turn, mode)).ToList();
        protocol.Conversation(stored.Mode, stored.Repeat, stored.Task, stored.Text("role"),
            ConversationMetrics.Of(facts, seconds), seconds, grades, outcome, stored.Number("errors") ?? 0);
    }
}
