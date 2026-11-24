using LouisAgent.Choices;
using LouisAgent.Composition.Agents;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Runs.Answers;
using LouisAgent.Evals.Contracts.Transcripts;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Turns;
using LouisAgent.Evals.Sessions;
using LouisAgent.Evals.Transcripts;
using LouisAgent.SessionStream;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Evals.Runs;

/// <summary>
/// Plays one conversation once against the agent, as a person would: one session, each ask in turn, the session
/// closed. A conversation that names a page, or gives one in the file, sends it with its first ask, as the site's
/// widget would (F126.3.3, F126.5.1). Then
/// it grades every turn with the turn grader, its facts by code and its criteria by the judge, and hands back the
/// repeat.
/// </summary>
/// <remarks>
/// A pipeline: a conversation goes in and a graded repeat comes out, asking first and grading after, so one place
/// opens and closes the session and the judge reads the same turns either way. A turn that ends in the agent's own
/// error is still a turn, since that is the agent failing and what an eval is for, but a recording that no longer
/// answers is the harness failing, and this is the one place the two are told apart. A refusal from the API, a judge
/// that can't mark or an out-of-date recording is not caught here: stopping the run is the runner's.
/// </remarks>
/// <param name="sessions">The sessions API the agent is asked over.</param>
/// <param name="judge">The judge that marks each criterion.</param>
/// <param name="clock">What times each turn; the system's clock unless a test gives its own.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class ConversationPlayer(
    SessionClient sessions,
    PageClient pages,
    Judge judge,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly TurnGrader _grader = new(judge);

    /// <summary>The conversation played once as the agent at this address, and graded.</summary>
    public async Task<RepeatRun> PlayAsync(
        AgentAddress agent, Conversation conversation, RunTrial trial, CancellationToken cancellationToken = default)
    {
        var asked = await AskAllAsync(agent, conversation, trial, cancellationToken);

        // The judge's answers are kept in the play's trial beside the agent's, or played back from it, and its calls
        // are recorded under the play's run and task, so the ledger ties what judging cost to its run (F126.3.4).
        using var judging = JudgeReplay.Begin(trial);
        var playTags = new UsageTags(trial.Task, trial.Run, null);
        using var grading = UsageScope.Begin(UsagePurpose.Grading, tags: playTags);
        var transcripts = asked.Select(turn => turn.Transcript).ToList();
        var turns = new List<TurnRun>();
        for (int at = 0; at < asked.Count; at++)
            turns.Add(await _grader.GradeAsync(conversation.Turns[at], asked[at].Transcript, asked[at].Cost,
                transcripts.Take(at + 1).ToList(), null, cancellationToken));

        return new RepeatRun(trial.Repeat, turns);
    }

    /// <summary>
    /// Every ask sent in one session opened as the agent, in order; the session is closed whatever happens, with no
    /// token, so a cancelled run still lets it go. A live play records under its trial, and a replay asks for the
    /// recorded trial back, the model's answers included.
    /// </summary>
    private async Task<List<AskedTurn>> AskAllAsync(
        AgentAddress agent, Conversation conversation, RunTrial trial, CancellationToken cancellationToken)
    {
        var replay = trial.Replayed is { } recorded
            ? new ReplayRequest(WebReplayMode.Replay.Wire(), recorded, Model: true)
            : new ReplayRequest(WebReplayMode.Record.Wire(), null);
        var opening = new CreateSessionRequest(trial.Task, trial.Run, null, Role: agent.Role, Replay: replay,
            Identity: agent.Identity);
        string session = await sessions.OpenAsync(opening, cancellationToken);
        try
        {
            // The page goes with the first ask only, as the widget sends it once for a visitor who stays on one page.
            // It is asked after the session opens, since opening a recording session starts its trial afresh.
            var page = await PageOfAsync(conversation, trial, cancellationToken);
            var asked = new List<AskedTurn>();
            foreach (var turn in conversation.Turns)
            {
                asked.Add(await AskAsync(session, turn.Ask, page, cancellationToken));
                page = null;
            }

            return asked;
        }
        finally
        {
            await sessions.CloseAsync(session, CancellationToken.None);
        }
    }

    /// <summary>
    /// The page the conversation's first ask carries: the one it gives in the file, sent as is (F126.5.1), the one it
    /// names, asked of the site, or none.
    /// </summary>
    private async Task<PageRequest?> PageOfAsync(Conversation conversation, RunTrial trial,
        CancellationToken cancellationToken)
    {
        if (conversation.GivenPage is { } given) return given.AsSent();
        if (conversation.Page is not { } address) return null;

        return await pages.FindAsync(address, trial, cancellationToken);
    }

    /// <summary>One ask sent, with its page if it has one, its stream read whole and timed, and written down.</summary>
    private async Task<AskedTurn> AskAsync(string session, string ask, PageRequest? page,
        CancellationToken cancellationToken)
    {
        long started = _clock.GetTimestamp();
        var events = new List<SessionEvent>();
        await foreach (var streamEvent in sessions.SendAsync(session, ask, page, cancellationToken))
            events.Add(streamEvent);

        // A recording that no longer answers is the harness failing, not the agent: it stops the run.
        var miss = events.OfType<ErrorReported>().FirstOrDefault(error => error.Kind == StreamErrorKind.ReplayMiss);
        if (miss is not null) throw new RecordingOutOfDateException(TurnTranscripts.MessageOf(miss) ?? "");

        var transcript = TurnTranscripts.Of(ask, events, _clock.GetElapsedTime(started));
        return new AskedTurn(transcript, TurnTranscripts.CostOf(events));
    }

    /// <summary>A turn as it was asked, before it is graded.</summary>
    private sealed record AskedTurn(TurnTranscript Transcript, TurnCost Cost);
}
