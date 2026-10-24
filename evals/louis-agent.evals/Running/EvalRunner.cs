using System.Text;
using System.Text.Json;
using LouisAgent.Core.Logging;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging;
using LouisAgent.Evals.Datasets;

namespace LouisAgent.Evals.Running;

/// <summary>
/// Runs an eval (F18 step 2): each mode, each repeat, each scenario; each message is sent to the API and the session's events
/// are written as they arrive, then the turn, the conversation's scores and the mode's end. The runner writes no grades yet:
/// each eval's graders are checked by the catalogue and run by the conformance test (F18 step 4.2).
/// </summary>
public sealed class EvalRunner(ISessionClient sessions, ProtocolWriter protocol, TimeProvider clock, ILogger<EvalRunner> logger)
{
    public async Task RunAsync(IEval eval, EvalRun run, CancellationToken cancellationToken)
    {
        var modes = await eval.ModesAsync(sessions, cancellationToken);
        logger.Log(LogLevel.Information, EvalLogMessage.RunStarted,
            new { Eval = eval.Name, Modes = modes.Count, Repeats = run.Repeats, Run = run.RunId });
        protocol.Begin(eval.Name, eval.Version, EvalBuild.Commit, eval.Graders,
            modes.Select(m => new ModelUse(m.Name, ModelLevel.Agent, m.Role, null)).ToList());   // the model isn't reported yet: the session client that could read it is the HTTP client, which is step 5

        foreach (var mode in modes)
            for (int repeat = 1; repeat <= run.Repeats; repeat++)
            {
                protocol.Mode(mode.Name, repeat, mode.Toolsets, mode.Role, mode.Llm, eval.Scenarios.Count);
                foreach (var scenario in eval.Scenarios)
                    await RunScenarioAsync(eval, run, mode, repeat, scenario, cancellationToken);
                protocol.ModeDone(mode.Name, repeat);
            }
    }

    private async Task RunScenarioAsync(IEval eval, EvalRun run, EvalMode mode, int repeat, EvalScenario scenario,
        CancellationToken cancellationToken)
    {
        if (scenario.Input.Messages.Count == 0)
        {
            logger.Log(LogLevel.Warning, EvalLogMessage.NoMessages, new { Scenario = scenario.Id });
            return;
        }

        var started = clock.GetUtcNow();
        string session = await sessions.OpenAsync(
            new SessionRequest(mode, scenario.Input.Place, scenario.Input.TimeZone, scenario.Id, repeat, $"{run.RunId}:{mode.Name}:{repeat}"),
            cancellationToken);

        // The session is closed however its turns end: a turn that throws (a timeout, a dead connection) must not leave it open.
        // The close is not cancelled with the run, so a cancelled run still closes what it opened.
        var turns = new List<TurnTranscript>();
        var facts = new List<TurnFacts>();
        try
        {
            for (int number = 1; number <= scenario.Input.Messages.Count; number++)
            {
                var turn = await RunTurnAsync(eval, mode, repeat, scenario.Id, number, session, scenario.Input.Messages[number - 1],
                    cancellationToken);
                turns.Add(turn.Transcript);
                facts.Add(turn.Facts);
            }
        }
        finally
        {
            // A failed close is logged, not thrown: it would replace the turn's own error, which is the one to report.
            try
            {
                await sessions.CloseAsync(session, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Warning, EvalLogMessage.CloseFailed, new { Session = session, Reason = ex.Message }, exception: ex);
            }
        }

        var trial = new Trial(run.RunId, mode, repeat, scenario, new Transcript(turns));
        var scores = eval.ScoresOf(trial, []);
        // The eval's own record, serialized as its runtime type: as EvalScores, which has no properties, it would be {}.
        // The conversation's role is the one that answered its first turn; its errors are the turns that failed.
        protocol.Conversation(mode.Name, repeat, scenario.Id, facts[0].Role, JsonSerializer.SerializeToElement<object>(scores, EvalScores.Json),
            (clock.GetUtcNow() - started).TotalSeconds, grades: null, outcome: null, errors: facts.Count(f => f.Error is not null));
    }

    private async Task<(TurnTranscript Transcript, TurnFacts Facts)> RunTurnAsync(IEval eval, EvalMode mode, int repeat, string task, int number,
        string session, string message,
        CancellationToken cancellationToken)
    {
        protocol.Question(mode.Name, repeat, task, number, session, message);

        var started = clock.GetUtcNow();
        var events = new List<SessionEvent>();
        var answer = new StringBuilder();
        await foreach (var streamEvent in sessions.SendAsync(session, message, cancellationToken))
        {
            events.Add(streamEvent);
            protocol.Sse(mode.Name, repeat, task, number, streamEvent.Type, streamEvent.Data);
            if (streamEvent is ContentBlockDelta { Data: var data }
                && data.TryGetProperty("delta", out var delta) && delta.ValueKind == JsonValueKind.Object
                && delta.TryGetProperty("type", out var type) && type.GetString() == "text_delta"
                && delta.TryGetProperty("text", out var text))
                answer.Append(text.GetString());
        }

        var duration = clock.GetUtcNow() - started;
        var transcript = new TurnTranscript(number, session, message, events, duration, null);
        var facts = eval.TurnFactsOf(transcript, mode);
        protocol.Turn(mode.Name, repeat, task, number, session, message, answer.ToString().Trim(), facts.Role, duration.TotalSeconds, facts);
        return (transcript, facts);
    }
}
