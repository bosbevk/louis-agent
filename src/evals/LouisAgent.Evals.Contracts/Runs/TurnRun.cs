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
}
