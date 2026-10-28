using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using MongoDB.Bson;
using static LouisAgent.Api.Evals.Store.Mongo.BsonValues;

namespace LouisAgent.Api.Evals.Store.Mongo.Documents;

/// <summary>
/// A run's slices as MongoDB keeps them, with the runner's fields as it wrote them. A rate a slice had too few trials
/// for stays empty, never 0, and so does a grader's mean score when it gave none.
/// </summary>
/// <remarks>
/// Embedded Value (PoEAA): the slices are a small summary of the run, so they live inside its document. Taken out of
/// the Mongo store (Fowler's Extract Class), which was past 200 lines.
/// </remarks>
internal static class SliceDocuments
{
    /// <summary>Writes the slices as an array of documents; none known is a null, never an empty array.</summary>
    public static BsonValue ToBson(IReadOnlyList<EvalSlice>? slices) =>
        slices is null
            ? BsonNull.Value
            : new BsonArray(slices.Select(s => new BsonDocument
            {
                ["mode"] = s.Mode,
                ["slice"] = s.Slice,
                ["n"] = s.N,
                ["passed"] = s.Passed,
                ["failed"] = s.Failed,
                ["unknown"] = s.Unknown,
                ["infrastructure"] = s.Infrastructure,
                ["tasks"] = new BsonArray(s.Tasks),
                ["too_few"] = s.TooFew,
                ["pass_rate"] = Nullable(s.PassRate),
                ["pass_at_k"] = Nullable(s.PassAtK),
                ["pass_hat_k"] = Nullable(s.PassHatK),
                ["graders"] = new BsonArray(s.Graders.Select(g => new BsonDocument
                {
                    ["grader"] = g.Grader,
                    ["kind"] = g.Kind.Wire(),
                    ["passed"] = g.Passed,
                    ["failed"] = g.Failed,
                    ["unknown"] = g.Unknown,
                    ["score_mean"] = Nullable(g.ScoreMean)
                })),
                ["kinds"] = new BsonArray(s.Kinds.Select(k => new BsonDocument
                {
                    ["kind"] = k.Kind.Wire(),
                    ["passed"] = k.Passed,
                    ["failed"] = k.Failed,
                    ["unknown"] = k.Unknown
                })),
                ["at"] = Date(s.At),
            }));

    /// <summary>Reads the slices back; a run from before them, or a null, has none.</summary>
    public static IReadOnlyList<EvalSlice>? Of(BsonValue value) =>
        value is not { IsBsonArray: true }
            ? null
            : value.AsBsonArray.Select(v => v.AsBsonDocument).Select(SliceOf).ToArray();

    /// <summary>One slice back from its document.</summary>
    private static EvalSlice SliceOf(BsonDocument d) =>
        new(d["mode"].AsString, d["slice"].AsString, d["n"].AsInt32, d["passed"].AsInt32, d["failed"].AsInt32,
            d["unknown"].AsInt32, d["infrastructure"].AsInt32,
            d["tasks"].AsBsonArray.Select(t => t.AsString).ToArray(), d["too_few"].AsBoolean,
            Number(d["pass_rate"]), Number(d["pass_at_k"]), Number(d["pass_hat_k"]),
            d["graders"].AsBsonArray.Select(g => g.AsBsonDocument).Select(GraderOf).ToArray(),
            d["kinds"].AsBsonArray.Select(k => k.AsBsonDocument).Select(KindOf).ToArray(),
            DateOf(d.GetValue("at", BsonNull.Value)));

    /// <summary>One grader's count back from its document.</summary>
    private static GraderCount GraderOf(BsonDocument g) =>
        new(g["grader"].AsString, Kind(g["kind"]), g["passed"].AsInt32, g["failed"].AsInt32, g["unknown"].AsInt32,
            Number(g["score_mean"]));

    /// <summary>One kind's count back from its document.</summary>
    private static KindCount KindOf(BsonDocument k) =>
        new(Kind(k["kind"]), k["passed"].AsInt32, k["failed"].AsInt32, k["unknown"].AsInt32);

    /// <summary>Keeps a rate we don't know as a null in Mongo, so it never turns into a 0.</summary>
    private static BsonValue Nullable(double? value) => value is null ? BsonNull.Value : new BsonDouble(value.Value);

    /// <summary>Reads a rate back, with a null staying unknown.</summary>
    private static double? Number(BsonValue value) => value.IsBsonNull ? null : value.ToDouble();

    /// <summary>Reads a grader's kind back as its enum, and stops on a name we don't know (ADR 0024).</summary>
    private static GraderKind Kind(BsonValue value) =>
        SettingChoice.TryParse(value.AsString, out GraderKind? kind) && kind is { } known
            ? known
            : throw new FormatException(value.AsString);
}
