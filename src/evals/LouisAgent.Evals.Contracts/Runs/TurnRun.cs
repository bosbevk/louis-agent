using LouisAgent.Evals.Contracts.Grading;
using LouisAgent.Evals.Contracts.Transcripts;

namespace LouisAgent.Evals.Contracts.Runs;

/// <summary>
/// One turn of a played conversation: what happened in it, a mark for each of its facts and each of its criteria,
/// and what it cost.
/// </summary>
/// <param name="Transcript">What was asked, answered and called in the turn.</param>
/// <param name="Facts">A mark for each fact, in the file's order, told by code.</param>
/// <param name="Criteria">A mark for each criterion, in the file's order, from the judge.</param>
/// <param name="Cost">What the agent's turn cost, or null when nobody could price it, never 0.</param>
/// <param name="Currency">The currency of the cost, or null with no cost.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record TurnRun(
    TurnTranscript Transcript,
    IReadOnlyList<Mark> Facts,
    IReadOnlyList<Mark> Criteria,
    decimal? Cost,
    string? Currency)
{
    /// <summary>
    /// What judging this turn's criteria cost, the known ones added: the run's second bill, beside the agent's own
    /// <see cref="Cost" />. Null when no criterion's cost is known, and absent from a run written before it was kept.
    /// </summary>
    /// <remarks>An init property, not a constructor parameter, so a run's file that has none still reads.</remarks>
    public decimal? JudgeCost { get; init; }

    /// <summary>
    /// This turn with one criterion flipped, counted from 1, or put back with none; none for a criterion it doesn't
    /// have. A fact is never flipped, since code tells it.
    /// </summary>
    public TurnRun? WithFlip(int criterion, Flip? flip) =>
        criterion >= 1 && criterion <= Criteria.Count
            ? this with { Criteria = Replace(Criteria, criterion - 1, Criteria[criterion - 1] with { Flip = flip }) }
            : null;

    /// <summary>The list with the item at that place swapped for another, the rest as they were.</summary>
    internal static IReadOnlyList<T> Replace<T>(IReadOnlyList<T> items, int at, T item) =>
        [.. items.Take(at), item, .. items.Skip(at + 1)];
}
