using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Runs.Answers;

namespace LouisAgent.Evals.Contracts.Runs;

/// <summary>
/// One play of a conversation, in a session of its own: its turns, each graded. It passed when every mark of every
/// turn is met, and a page asks it that, never working it out where it is shown.
/// </summary>
/// <remarks>
/// What a page asks of a repeat is a method here, never a property, so nothing new is written into the run's file:
/// the page's sums on the run's records, chosen over a presentation model per page (Fowler) and over static helpers
/// in the web project (Louis, 2026-11-23, by ADR 0123). The marks are counted here alone; a run adds its repeats'.
/// </remarks>
/// <param name="Number">Which repeat this is, from 1.</param>
/// <param name="Turns">The conversation's turns, in order, each with its marks.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0123-ask-the-type.md">ADR 0123: ask the type</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed record RepeatRun(int Number, IReadOnlyList<TurnRun> Turns)
{
    /// <summary>Every fact and every criterion of every turn was met.</summary>
    public bool Passed => Turns.All(turn => turn.Facts.All(mark => mark.Met) && turn.Criteria.All(mark => mark.Met));

    /// <summary>Its facts over all its turns, met of all.</summary>
    public MarkCount CountFacts() => Count(Turns.SelectMany(turn => turn.Facts));

    /// <summary>Its criteria over all its turns, met of all.</summary>
    public MarkCount CountCriteria() => Count(Turns.SelectMany(turn => turn.Criteria));

    /// <summary>What judging its turns cost, the known ones added; null when none is known, never 0.</summary>
    public decimal? SumJudgeCost() => KnownCost.Sum(Turns.Select(turn => turn.JudgeCost));

    /// <summary>What the agent's turns cost, the known ones added; null when none has a price.</summary>
    public decimal? SumCost() => KnownCost.Sum(Turns.Select(turn => turn.Cost));

    /// <summary>How long its turns took, added.</summary>
    public TimeSpan SumTime() => Turns.Aggregate(TimeSpan.Zero, (sum, turn) => sum + turn.Transcript.Took);

    /// <summary>
    /// The first mark not met, turns in order and a turn's facts before its criteria; none when the repeat passed.
    /// </summary>
    public Miss? FindFirstMiss() => Turns
        .SelectMany((turn, at) =>
            turn.Facts.Select(mark => new Miss(at + 1, MarkKind.Fact, mark))
                .Concat(turn.Criteria.Select(mark => new Miss(at + 1, MarkKind.Criterion, mark))))
        .FirstOrDefault(miss => !miss.Mark.Met);

    /// <summary>These marks, met of all.</summary>
    private static MarkCount Count(IEnumerable<Mark> marks)
    {
        var counted = marks.ToList();
        return new MarkCount(counted.Count(mark => mark.Met), counted.Count);
    }
}
