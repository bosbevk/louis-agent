using System.Text.Json.Serialization;

namespace LouisAgent.Api.Contracts.Admin.Runs;

/// <summary>
/// One slice of a mode's roll-up (F18 step 5c.2): the runner's counts and rates as it wrote them, and what a trial and
/// a pass cost from the ledger. A rate the runner left out (too few trials) and an unknown cost per trial are null,
/// never 0; the cost per pass is left out when it is null, which is when nothing passed or a price was unknown (a
/// reader tells them apart by the passed count).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
public sealed record SummaryRow(
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
    IReadOnlyList<SliceGraderRow> Graders,
    IReadOnlyList<SliceKindRow> Kinds,
    IReadOnlyList<SliceScoreRow> Scores,
    decimal? CostPerTrial,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    decimal? CostPerPass);

/// <summary>
/// One grader's counts in a slice, and its mean score over the slice's counted trials (null when it gave none).
/// </summary>
public sealed record SliceGraderRow(
    string Grader,
    string Kind,
    int Passed,
    int Failed,
    int Unknown,
    double? ScoreMean = null);

/// <summary>One kind of grader's counts in a slice.</summary>
public sealed record SliceKindRow(string Kind, int Passed, int Failed, int Unknown);

/// <summary>
/// One score's mean in a slice, over how many trials. No slice has any since protocol version 2; it goes in F18 step
/// 7c.5 with the portal code that reads it.
/// </summary>
public sealed record SliceScoreRow(string Key, double Mean, int N);
