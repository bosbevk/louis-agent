using LouisAgent.Composition.Messages;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Turns;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Messages.Holes.Runs;
using LouisAgent.Evals.Transcripts;

namespace LouisAgent.Evals.Runs;

/// <summary>
/// Grades a run's stored answers again, against the eval file as it is now, with no agent asked. Every fact is told
/// again in code, which is free; every mark whose turn and criterion's words are the same is kept, flip and note
/// with it; the judge is asked only for a criterion that is new or reworded. Its answers are recorded under the live
/// run's play, beside the ones recorded there already, so a replay of that live run finds them too.
/// </summary>
/// <remarks>
/// A re-grade is a new run, as Inspect's <c>inspect score</c> writes a new log and W&amp;B Weave's rescore a new
/// evaluation run, so the run it re-graded stays as it was. Its judge answers go into the live run's recording
/// (Louis's pick, 2026-11-23, "Into the live run's recording"), VCR's new-episodes mode: the store appends a new
/// answer to the play's index and keeps the old ones. A conversation the run doesn't hold, or whose asks changed,
/// is left out, since no answer to it is stored. A judge that can't mark ends it as could not finish, as it ends a run.
/// </remarks>
/// <param name="grader">How a turn is graded, the way the player grades one.</param>
/// <param name="clock">The time the re-grade ends at.</param>
/// <seealso href="https://inspect.aisi.org.uk/scoring-workflow.html">Inspect: the scoring workflow</seealso>
/// <seealso href="https://nicolasiensen.gitbook.io/vcr/record-modes/new_episodes">VCR: the new_episodes record mode</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class Regrader(TurnGrader grader, TimeProvider clock)
{
    /// <summary>
    /// The re-grade played to its end from how it started, each conversation of the file in its order that the graded
    /// run holds with the same asks, handed to <paramref name="kept" /> after each one and as it ends.
    /// </summary>
    /// <param name="started">The re-grade as it starts, naming the run it re-grades and the live run behind it.</param>
    /// <param name="graded">The run whose stored answers are graded again.</param>
    /// <param name="file">The eval file as it is now.</param>
    /// <param name="kept">Keeps the re-grade as it goes.</param>
    /// <param name="cancellationToken">A person's stop, or the host's shutdown.</param>
    public async Task<EvalRun> RegradeAsync(EvalRun started, EvalRun graded, EvalFile file, Func<EvalRun, Task> kept,
        CancellationToken cancellationToken = default)
    {
        // The live run whose answers these are, which its plays' recordings are kept under.
        string live = graded.ReplayOf ?? graded.Id;
        var regraded = new List<ConversationRun>();
        foreach (var conversation in file.Conversations)
        {
            if (Stored(graded, conversation) is not { } stored) continue;
            try
            {
                regraded.Add(await RegradeAsync(started, live, conversation, stored, cancellationToken));
            }
            catch (JudgeFailedException failed)
            {
                var judgeFailed = new JudgeFailedHoles(failed.Message);
                return await EndAsync(started, regraded, RunStatus.CouldNotFinish,
                    MessageText.Format(RunMessage.JudgeFailed, judgeFailed), kept);
            }

            await kept(started with { Conversations = [.. regraded] });
        }

        return await EndAsync(started, regraded, RunStatus.Finished, null, kept);
    }

    /// <summary>
    /// The graded run's conversation, when it holds it and every repeat asked what the file asks now; none otherwise,
    /// since no answer to a changed ask is stored.
    /// </summary>
    private static ConversationRun? Stored(EvalRun graded, Conversation conversation)
    {
        var stored = graded.Conversations.FirstOrDefault(kept => kept.Id == conversation.Id);
        var asks = conversation.Turns.Select(turn => turn.Ask).ToList();
        bool sameAsks = stored is not null && stored.Repeats.All(repeat =>
            repeat.Turns.Select(turn => turn.Transcript.Ask).SequenceEqual(asks));
        return sameAsks ? stored : null;
    }

    /// <summary>One conversation's repeats graded again, each under the live run's play of it.</summary>
    private async Task<ConversationRun> RegradeAsync(EvalRun started, string live, Conversation conversation,
        ConversationRun stored, CancellationToken cancellationToken)
    {
        var repeats = new List<RepeatRun>();
        foreach (var repeat in stored.Repeats)
        {
            // The judge records into the live run's play, beside its answers there (a trial with no replay records).
            var play = new RunTrial(live, conversation.Id, repeat.Number);
            using var judging = JudgeReplay.Begin(play);
            // Its calls are priced under the re-grade's own run and the play's task.
            var tags = new UsageTags(play.Task, started.Id, null);
            using var grading = UsageScope.Begin(UsagePurpose.Grading, tags: tags);
            repeats.Add(await RegradeAsync(conversation, repeat, cancellationToken));
        }

        return new ConversationRun(conversation.Id, stored.Asked, repeats);
    }

    /// <summary>A repeat's turns graded again from their stored transcripts; no agent was asked: a known 0.</summary>
    private async Task<RepeatRun> RegradeAsync(Conversation conversation, RepeatRun repeat,
        CancellationToken cancellationToken)
    {
        var transcripts = repeat.Turns.Select(turn => turn.Transcript).ToList();
        var turns = new List<TurnRun>();
        for (int at = 0; at < repeat.Turns.Count; at++)
        {
            var before = repeat.Turns[at];
            var noAgent = new TurnCost(0m, before.Currency);
            turns.Add(await grader.GradeAsync(conversation.Turns[at], before.Transcript, noAgent,
                transcripts.Take(at + 1).ToList(), before, cancellationToken));
        }

        return new RepeatRun(repeat.Number, turns);
    }

    /// <summary>The re-grade ended as it ended, at the clock's time, and kept once more.</summary>
    private async Task<EvalRun> EndAsync(EvalRun started, List<ConversationRun> regraded, RunStatus status,
        string? stoppedBecause, Func<EvalRun, Task> kept)
    {
        var ended = started with
        {
            Conversations = [.. regraded],
            Status = status,
            EndedAt = clock.GetUtcNow(),
            StoppedBecause = stoppedBecause
        };
        await kept(ended);
        return ended;
    }
}
