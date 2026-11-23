using LouisAgent.Composition.Messages;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Runs;
using LouisAgent.Evals.Sessions;

namespace LouisAgent.Evals.Runs;

/// <summary>
/// Runs an eval: plays every conversation in the file as many times as the run asks, keeps the run after each
/// conversation so its file is its progress, and ends it as finished. When the harness fails, it ends the run as
/// could not finish and says why, keeping what was graded before.
/// </summary>
/// <remarks>
/// The second half of the pipeline: the player knows the sessions API, the runner knows repeats and stopping. A
/// conversation passes only when all its repeats pass, chosen over at least one passing (Louis, 2026-11-22). The run
/// stops when the judge can't mark, chosen over a third kind of mark and over a retry (Louis, 2026-11-23), and when
/// the API refuses, since in both nothing was learnt about the agent. Only those two are caught: anything else is a
/// bug and surfaces, and a cancellation stays a cancellation.
/// </remarks>
/// <param name="player">What plays one conversation once.</param>
/// <param name="clock">The time a run ends at.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class EvalRunner(ConversationPlayer player, TimeProvider clock)
{
    /// <summary>
    /// The run played to its end from how it started, each conversation in the file's order, handed to
    /// <paramref name="kept"/> after each one and as it ends.
    /// </summary>
    public async Task<EvalRun> RunAsync(
        EvalRun started, EvalFile file, Func<EvalRun, Task> kept, CancellationToken cancellationToken = default)
    {
        var graded = new List<ConversationRun>();
        foreach (var conversation in file.Conversations)
        {
            var played = await PlayAsync(started, file, conversation, cancellationToken);
            if (played.Run.Repeats.Count > 0) graded.Add(played.Run);
            if (played.StoppedBecause is { } reason)
                return await EndAsync(started, graded, RunStatus.CouldNotFinish, reason, kept);

            await kept(started with { Conversations = [.. graded] });
        }

        return await EndAsync(started, graded, RunStatus.Finished, null, kept);
    }

    /// <summary>
    /// One conversation played as many times as the run asks, each in a session of its own; when the harness fails,
    /// the repeats played so far, and why it stopped.
    /// </summary>
    private async Task<PlayedConversation> PlayAsync(
        EvalRun started, EvalFile file, Conversation conversation, CancellationToken cancellationToken)
    {
        var repeats = new List<RepeatRun>();
        try
        {
            for (int repeat = 1; repeat <= started.Repeats; repeat++)
            {
                var trial = new RunTrial(started.Id, conversation.Id, repeat);
                repeats.Add(await player.PlayAsync(file.Agent, conversation, trial, cancellationToken));
            }

            return new PlayedConversation(new ConversationRun(conversation.Id, started.Repeats, repeats), null);
        }
        catch (JudgeFailedException failed)
        {
            var judgeFailed = new JudgeFailedHoles(failed.Message);
            return new PlayedConversation(new ConversationRun(conversation.Id, started.Repeats, repeats),
                MessageText.Format(RunMessage.JudgeFailed, judgeFailed));
        }
        catch (SessionRefusedException refused)
        {
            var sessionRefused = new SessionRefusedHoles(refused.Reason);
            return new PlayedConversation(new ConversationRun(conversation.Id, started.Repeats, repeats),
                MessageText.Format(RunMessage.SessionRefused, sessionRefused));
        }
    }

    /// <summary>The run ended as it ended, at the clock's time, and kept once more.</summary>
    private async Task<EvalRun> EndAsync(EvalRun started, List<ConversationRun> graded, RunStatus status,
        string? stoppedBecause, Func<EvalRun, Task> kept)
    {
        var ended = started with
        {
            Conversations = [.. graded],
            Status = status,
            EndedAt = clock.GetUtcNow(),
            StoppedBecause = stoppedBecause
        };
        await kept(ended);
        return ended;
    }

    /// <summary>A conversation as far as it was played, and why it stopped when the harness failed.</summary>
    private sealed record PlayedConversation(ConversationRun Run, string? StoppedBecause);
}
