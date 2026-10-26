using System.Text.Json.Serialization;
using LouisAgent.Api.Benchmarks.Reports;
using LouisAgent.Api.Benchmarks.Runs;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Admin;

/// <summary>One grader's counts in a slice's row.</summary>
internal sealed record GraderRow([property: JsonPropertyName("grader")] string Grader, [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("passed")] int Passed, [property: JsonPropertyName("failed")] int Failed, [property: JsonPropertyName("unknown")] int Unknown);

/// <summary>One kind's counts in a slice's row.</summary>
internal sealed record KindRow([property: JsonPropertyName("kind")] string Kind, [property: JsonPropertyName("passed")] int Passed,
    [property: JsonPropertyName("failed")] int Failed, [property: JsonPropertyName("unknown")] int Unknown);

/// <summary>One score's mean in a slice's row.</summary>
internal sealed record ScoreRow([property: JsonPropertyName("key")] string Key, [property: JsonPropertyName("mean")] double Mean, [property: JsonPropertyName("n")] int N);

/// <summary>
/// One slice as the admin endpoint sends it (F18 step 5c.2): the runner's fields as it wrote them, and the ledger's cost (<see cref="SlicePricer"/>). A record the serializer writes
/// (ADR 0041), its names snake_case like the other rows'. A rate the runner left out (too few trials) and an unknown cost per trial are null, never 0; the cost per pass is left out
/// when null, which is when nothing passed or a price was unknown (the reader tells them apart by the passed count).
/// </summary>
internal sealed record SummaryRow(
    [property: JsonPropertyName("mode")] string Mode, [property: JsonPropertyName("slice")] string Slice, [property: JsonPropertyName("n")] int N,
    [property: JsonPropertyName("passed")] int Passed, [property: JsonPropertyName("failed")] int Failed, [property: JsonPropertyName("unknown")] int Unknown,
    [property: JsonPropertyName("infrastructure")] int Infrastructure, [property: JsonPropertyName("tasks")] IReadOnlyList<string> Tasks,
    [property: JsonPropertyName("too_few")] bool TooFew, [property: JsonPropertyName("pass_rate")] double? PassRate, [property: JsonPropertyName("pass_at_k")] double? PassAtK,
    [property: JsonPropertyName("pass_hat_k")] double? PassHatK, [property: JsonPropertyName("graders")] IReadOnlyList<GraderRow> Graders,
    [property: JsonPropertyName("kinds")] IReadOnlyList<KindRow> Kinds, [property: JsonPropertyName("scores")] IReadOnlyList<ScoreRow> Scores,
    [property: JsonPropertyName("cost_per_trial")] decimal? CostPerTrial,
    [property: JsonPropertyName("cost_per_pass"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? CostPerPass)
{
    /// <summary>A stored run's roll-up, each slice priced from the ledger; null for a run from before 5c (never an empty list or zeros).</summary>
    public static IReadOnlyList<SummaryRow>? Of(IReadOnlyList<BenchmarkSlice>? slices, Total? total, IReadOnlyList<Request> requests) =>
        slices?.Select(slice => Row(slice, SlicePricer.Of(slice, total, requests))).ToList();

    private static SummaryRow Row(BenchmarkSlice s, SlicePricer.SliceCost cost) =>
        new(s.Mode, s.Slice, s.N, s.Passed, s.Failed, s.Unknown, s.Infrastructure, s.Tasks, s.TooFew, s.PassRate, s.PassAtK, s.PassHatK,
            [.. s.Graders.Select(g => new GraderRow(g.Grader, g.Kind.ToString(), g.Passed, g.Failed, g.Unknown))],
            [.. s.Kinds.Select(k => new KindRow(k.Kind.ToString(), k.Passed, k.Failed, k.Unknown))],
            [.. s.Scores.Select(c => new ScoreRow(c.Key, c.Mean, c.N))], cost.PerTrial, cost.PerPass);
}
