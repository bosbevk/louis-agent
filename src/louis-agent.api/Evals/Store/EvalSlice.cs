using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Api.Evals.Store;

/// <summary>
/// One slice of a mode's roll-up, its <c>overall</c> line or one category, as the runner's <c>summary</c> line wrote
/// it. We keep the runner's numbers and work out no rate ourselves; a rate is null when the slice had too few trials,
/// never 0. Each grader's count carries its mean score, the partial credit a pass or a fail hides.
/// </summary>
/// <remarks>
/// The stored slice is its own record and not the summary event, since a run keeps it without the line's time.
/// Fowler's Replace Constructor with Factory Function: <see cref="Of"/> builds it from the event, so the recorder reads
/// as one call. No cost here on purpose: the ledger prices a slice when it's read (<c>SlicePricer</c>), so an export
/// carries no cost and grading it again can't smuggle an old cost into a new run.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record EvalSlice(
    string Mode,
    string Slice,
    int N,
    int Passed,
    int Failed,
    int Unknown,
    int Infrastructure,
    IReadOnlyList<string> Tasks,
    bool TooFew,
    double? PassRate,
    double? PassAtK,
    double? PassHatK,
    IReadOnlyList<GraderCount> Graders,
    IReadOnlyList<KindCount> Kinds,
    DateTimeOffset? At = null)
{
    /// <summary>The slice a summary line reported, and when it reported it.</summary>
    public static EvalSlice Of(SummaryEvent summary) =>
        new(summary.Mode, summary.Slice, summary.N, summary.Passed, summary.Failed, summary.Unknown,
            summary.Infrastructure, summary.Tasks, summary.TooFew, summary.PassRate, summary.PassAtK, summary.PassHatK,
            summary.Graders, summary.Kinds, summary.T);

    /// <summary>
    /// The summary line this slice gives back, at its own time, or at the given one for a slice stored without.
    /// </summary>
    public SummaryEvent ToEvent(DateTimeOffset fallback) =>
        new(At ?? fallback, Mode, Slice, N, Passed, Failed, Unknown, Infrastructure, Tasks, TooFew, PassRate, PassAtK,
            PassHatK, Graders, Kinds);
}
