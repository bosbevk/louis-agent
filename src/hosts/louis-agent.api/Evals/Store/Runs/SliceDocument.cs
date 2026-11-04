using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using MongoDB.Bson.Serialization.Attributes;

namespace LouisAgent.Api.Evals.Store.Runs;

/// <summary>
/// One slice of a mode's roll-up inside a run's document, with the runner's fields as it wrote them. A rate a slice
/// had too few trials for stays null, never 0, and so does a grader's mean score when it gave none.
/// </summary>
/// <remarks>
/// Embedded Value (PoEAA): the slices are a small summary of the run, so they live inside its document. Metadata
/// Mapping on a Data Transfer Object, as <see cref="RunDocument"/> explains.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record SliceDocument(
    [property: BsonElement("mode")] string Mode,
    [property: BsonElement("slice")] string Slice,
    [property: BsonElement("n")] int N,
    [property: BsonElement("passed")] int Passed,
    [property: BsonElement("failed")] int Failed,
    [property: BsonElement("unknown")] int Unknown,
    [property: BsonElement("infrastructure")]
    int Infrastructure,
    [property: BsonElement("tasks")] IReadOnlyList<string> Tasks,
    [property: BsonElement("too_few")] bool TooFew,
    [property: BsonElement("pass_rate")] double? PassRate,
    [property: BsonElement("pass_at_k")] double? PassAtK,
    [property: BsonElement("pass_hat_k")] double? PassHatK,
    [property: BsonElement("graders")] IReadOnlyList<SliceGraderDocument> Graders,
    [property: BsonElement("kinds")] IReadOnlyList<SliceKindDocument> Kinds,
    [property: BsonElement("at")] DateTimeOffset? At)
{
    /// <summary>The document for a slice.</summary>
    public static SliceDocument Of(EvalSlice s) =>
        new(s.Mode, s.Slice, s.N, s.Passed, s.Failed, s.Unknown, s.Infrastructure, s.Tasks, s.TooFew, s.PassRate,
            s.PassAtK, s.PassHatK, [.. s.Graders.Select(SliceGraderDocument.Of)],
            [.. s.Kinds.Select(SliceKindDocument.Of)], s.At);

    /// <summary>The slice this document holds.</summary>
    public EvalSlice ToSlice() =>
        new(Mode, Slice, N, Passed, Failed, Unknown, Infrastructure, Tasks, TooFew, PassRate, PassAtK, PassHatK,
            [.. Graders.Select(g => g.ToCount())], [.. Kinds.Select(k => k.ToCount())], At);
}

/// <summary>One grader's counts in a slice, and its mean score.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record SliceGraderDocument(
    [property: BsonElement("grader")] string Grader,
    [property: BsonElement("kind")] CheckKind Kind,
    [property: BsonElement("passed")] int Passed,
    [property: BsonElement("failed")] int Failed,
    [property: BsonElement("unknown")] int Unknown,
    [property: BsonElement("score_mean")] double? ScoreMean)
{
    /// <summary>The document for a grader's counts.</summary>
    public static SliceGraderDocument Of(GraderCount g) =>
        new(g.Grader, g.Kind, g.Passed, g.Failed, g.Unknown, g.ScoreMean);

    /// <summary>The grader's counts this document holds.</summary>
    public GraderCount ToCount() => new(Grader, Kind, Passed, Failed, Unknown, ScoreMean);
}

/// <summary>One kind of grader's counts in a slice.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
[BsonIgnoreExtraElements]
internal sealed record SliceKindDocument(
    [property: BsonElement("kind")] CheckKind Kind,
    [property: BsonElement("passed")] int Passed,
    [property: BsonElement("failed")] int Failed,
    [property: BsonElement("unknown")] int Unknown)
{
    /// <summary>The document for a kind's counts.</summary>
    public static SliceKindDocument Of(KindCount k) => new(k.Kind, k.Passed, k.Failed, k.Unknown);

    /// <summary>The kind's counts this document holds.</summary>
    public KindCount ToCount() => new(Kind, Passed, Failed, Unknown);
}
