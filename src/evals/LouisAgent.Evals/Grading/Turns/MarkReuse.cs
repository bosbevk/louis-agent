using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs;
using LouisAgent.Evals.Contracts.Transcripts;

namespace LouisAgent.Evals.Grading.Turns;

/// <summary>
/// The one rule for when a mark carries over from an earlier run: where a turn says the same as before
/// (<see cref="TurnTranscript.SaysTheSameAs" />) and a criterion's words are word for word the same, the earlier mark
/// is the mark, with a person's flip and note. A replay asks it now, and a re-grade will.
/// </summary>
/// <remarks>
/// A replay carries the live run's flips, same answer only (Louis's pick, 2026-11-23, "It carries them, same answer
/// only"), over carrying none and over carrying every flip by the criterion's words alone: a flip is a person's word
/// on one answer, so it holds only for that answer. One rule written once, so a replay and a re-grade can't disagree
/// on what "the same" is.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public static class MarkReuse
{
    /// <summary>
    /// The earlier turn's mark of this criterion, flip and note with it, when the turn now says the same as it did and
    /// the criterion's words are the same; none otherwise.
    /// </summary>
    public static Mark? FindReusable(TurnRun before, TurnTranscript now, string criterion) =>
        before.Transcript.SaysTheSameAs(now)
            ? before.Criteria.FirstOrDefault(mark => mark.Text == criterion)
            : null;

    /// <summary>
    /// A played repeat with the live run's flips carried onto it: each criterion whose turn says the same as the live
    /// one's, and whose judge's answer is the live one's, takes that mark's flip. With no live repeat it is as it was.
    /// </summary>
    public static RepeatRun CarryFlips(RepeatRun played, RepeatRun? live)
    {
        if (live is null) return played;

        var turns = played.Turns.Select((turn, at) => at < live.Turns.Count ? Carry(turn, live.Turns[at]) : turn);
        return played with { Turns = [.. turns] };
    }

    /// <summary>A played turn with the live turn's flips on the marks that carry over.</summary>
    private static TurnRun Carry(TurnRun played, TurnRun live)
    {
        var criteria = played.Criteria.Select(mark =>
            FindReusable(live, played.Transcript, mark.Text) is { Flip: { } flip } before && before.Met == mark.Met
                ? mark with { Flip = flip }
                : mark);
        return played with { Criteria = [.. criteria] };
    }
}
