using LouisAgent.Core.Logging;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Trials;
using LouisAgent.SessionStream;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Evals.Running;

/// <summary>
/// One go at a scenario: opens a session, sends each message and writes what comes back to the protocol, then closes
/// the session however the turns end. It says what the turns were and what the eval made of them; whether to try again
/// and how the trial is graded stay with <see cref="EvalRunner"/>.
/// </summary>
/// <remarks>
/// Extract Class. Talking to a session (open it, run its turns, close it, name its run tag) is a different job from
/// running an eval (the modes, the repeats, the retry, the grading), and it only needs the client, the protocol, the
/// clock and the log. Kept inside the runner, that job and its helpers took the runner past two hundred lines and hid
/// the flow.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0010-folders-by-concern.md">ADR 0010: folders by concern</seealso>
public sealed class ScenarioAttempt(
    ISessionClient sessions,
    ProtocolWriter protocol,
    TimeProvider clock,
    ILogger logger)
{
    /// <summary>One attempt at a scenario: a session, its turns, and the session closed however they end.</summary>
    public async Task<(List<TurnTranscript> Turns, List<TurnFacts> Facts)> RunAsync(IEval eval, EvalRun run,
        EvalMode mode, int repeat,
        EvalScenario scenario, CancellationToken cancellationToken)
    {
        // A session that can't open fails every turn of its scenario the same way, and there is nothing to close
        // (F18 step 5.1).
        string? session = null;
        TrialFailure? openFailure = null;
        try
        {
            session = await sessions.OpenAsync(
                new SessionRequest(mode, scenario.Input.Place, scenario.Input.TimeZone, scenario.Id, repeat,
                    RunTag(run.RunId, mode, repeat),
                    ReplayFor(run, mode, repeat, scenario)),
                cancellationToken);
        }
        catch (Exception ex) when (TurnFailures.Of(ex, cancellationToken) is { } failure)
        {
            openFailure = failure;
            logger.Log(LogLevel.Warning, EvalLogMessage.OpenFailed,
                new
                {
                    Scenario = scenario.Id,
                    Failure = failure,
                    Reason = ex.Message
                }, exception: ex);
        }

        // The session is closed however its turns end: a turn that throws (a timeout, a dead connection) must not leave
        // it open. The close is not cancelled with the run, so a cancelled run still closes what it opened.
        var turns = new List<TurnTranscript>();
        var facts = new List<TurnFacts>();
        try
        {
            for (int number = 1; number <= scenario.Input.Messages.Count; number++)
            {
                var turn = await RunTurnAsync(eval, mode, repeat, scenario.Id, number, session, openFailure,
                    scenario.Input.Messages[number - 1],
                    cancellationToken);
                turns.Add(turn.Transcript);
                facts.Add(turn.Facts);
            }
        }
        finally
        {
            // A failed close is logged, not thrown: it would replace the turn's own error, which is the one to report.
            if (session is not null)
                try
                {
                    await sessions.CloseAsync(session, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Warning, EvalLogMessage.CloseFailed,
                        new
                        {
                            Session = session,
                            Reason = ex.Message
                        }, exception: ex);
                }
        }

        return (turns, facts);
    }

    /// <summary>
    /// The run tag of one mode's repeat: its sessions carry it, and the ledger's rows find their run by it (F18 step
    /// 4a: a replay's trial key starts with it).
    /// </summary>
    private static string RunTag(string runId, EvalMode mode, int repeat) => $"{runId}:{mode.Name}:{repeat}";

    /// <summary>
    /// What a session sends for the web (F18 step 4a). Every eval session sends one, even when it names nothing: that
    /// is the mark of an eval. A recording leaves its trial out, so the API writes {run tag}/{task}; a replay names the
    /// trial the recorded run wrote, the key the API wrote there, from the same run tag and the scenario's id;
    /// <c>Model</c> asks for the model's answers from it too (step 4e).
    /// </summary>
    private static WebReplayRequest ReplayFor(EvalRun run, EvalMode mode, int repeat, EvalScenario scenario) =>
        run.Web == WebReplayMode.Replay && run.Of is { } recorded
            ? new WebReplayRequest(run.Web, $"{RunTag(recorded, mode, repeat)}/{scenario.Id}", run.Model)
            : new WebReplayRequest(run.Web, null, run.Model);

    /// <summary>
    /// One turn. A failure of the infrastructure (<see cref="TurnFailures"/>) ends it with its
    /// <see cref="TrialFailure"/>, keeping the events read before it; a turn whose session never opened
    /// (<paramref name="session"/> null) is that failure from the start. A cancelled run throws.
    /// </summary>
    private async Task<(TurnTranscript Transcript, TurnFacts Facts)> RunTurnAsync(IEval eval, EvalMode mode, int repeat,
        string task, int number,
        string? session, TrialFailure? openFailure, string message,
        CancellationToken cancellationToken)
    {
        protocol.Question(mode.Name, repeat, task, number, session ?? string.Empty, message);

        var started = clock.GetUtcNow();
        var events = new List<SessionEvent>();
        var failure = openFailure;
        if (session is not null)
            try
            {
                await foreach (var streamEvent in sessions.SendAsync(session, message, cancellationToken))
                {
                    events.Add(streamEvent);
                    protocol.Sse(mode.Name, repeat, task, number, streamEvent.Type, streamEvent.Data);
                }
            }
            catch (Exception ex) when (TurnFailures.Of(ex, cancellationToken) is { } turnFailure)
            {
                failure = turnFailure;
                logger.Log(LogLevel.Warning, EvalLogMessage.TurnFailed,
                    new
                    {
                        Scenario = task,
                        Turn = number,
                        Failure = turnFailure,
                        Reason = ex.Message
                    }, exception: ex);
            }

        var duration = clock.GetUtcNow() - started;
        var transcript = new TurnTranscript(number, session ?? string.Empty, message, events, duration, failure);
        var facts = eval.TurnFactsOf(transcript, mode);
        // The answer through the one reader of the engine's events: the runner's own loop called GetString() on each
        // delta's text, which throws on a number, so one odd delta stopped the run; AnswerOf skips it (F18 step 4.5a,
        // ADR 0030).
        protocol.Turn(mode.Name, repeat, task, number, transcript.Session, message, EngineEvents.AnswerOf(transcript),
            facts.Role, duration.TotalSeconds, facts, failure);
        return (transcript, facts);
    }
}
