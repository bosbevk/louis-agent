using LouisAgent.Evals.Contracts.Grading;

namespace LouisAgent.Evals.Grading;

/// <summary>
/// A criterion's mark, with what the judge's call for it cost: the ledger's price when the model records to one, a
/// known 0 when its answer was played back, and none when it isn't known.
/// </summary>
/// <param name="Mark">The criterion marked, met or not, with the judge's reason.</param>
/// <param name="Cost">What the call cost, or null when it isn't known, never 0 (ADR 0007).</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record JudgedMark(Mark Mark, decimal? Cost);
