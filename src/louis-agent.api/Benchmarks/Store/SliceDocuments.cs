using LouisAgent.Api.Benchmarks.Runs;
using MongoDB.Bson;
using LouisAgent.Choices;

namespace LouisAgent.Api.Benchmarks.Store;

/// <summary>
/// A run's slices as MongoDB keeps them (F18 step 5c.2), taken out of <see cref="MongoBenchmarkStore"/>, which was past 200 lines. The runner's fields as it wrote them: a rate a slice
/// had too few trials for is null, never 0, and a run stored before 5c has no <c>slices</c> and reads as none.
/// </summary>
internal static class SliceDocuments
{
    public static BsonValue ToBson(IReadOnlyList<BenchmarkSlice>? slices) =>
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
                    ["unknown"] = g.Unknown
                })),
                ["kinds"] = new BsonArray(s.Kinds.Select(k => new BsonDocument
                {
                    ["kind"] = k.Kind.Wire(),
                    ["passed"] = k.Passed,
                    ["failed"] = k.Failed,
                    ["unknown"] = k.Unknown
                })),
                ["scores"] = new BsonArray(s.Scores.Select(c => new BsonDocument
                { ["key"] = c.Key, ["mean"] = c.Mean, ["n"] = c.N })),
            }));

    public static IReadOnlyList<BenchmarkSlice>? Of(BsonValue value) =>
        value is not { IsBsonArray: true }
            ? null
            : value.AsBsonArray.Select(v => v.AsBsonDocument).Select(d => new BenchmarkSlice(
                d["mode"].AsString, d["slice"].AsString, d["n"].AsInt32, d["passed"].AsInt32, d["failed"].AsInt32,
                d["unknown"].AsInt32, d["infrastructure"].AsInt32,
                d["tasks"].AsBsonArray.Select(t => t.AsString).ToArray(), d["too_few"].AsBoolean,
                Number(d["pass_rate"]), Number(d["pass_at_k"]), Number(d["pass_hat_k"]),
                d["graders"].AsBsonArray.Select(g => g.AsBsonDocument).Select(g => new SliceGrader(g["grader"].AsString,
                    Kind(g["kind"]), g["passed"].AsInt32, g["failed"].AsInt32, g["unknown"].AsInt32)).ToArray(),
                d["kinds"].AsBsonArray.Select(k => k.AsBsonDocument).Select(k =>
                        new SliceKind(Kind(k["kind"]), k["passed"].AsInt32, k["failed"].AsInt32, k["unknown"].AsInt32))
                    .ToArray(),
                d["scores"].AsBsonArray.Select(c => c.AsBsonDocument).Select(c =>
                    new SliceScore(c["key"].AsString, c["mean"].ToDouble(), c["n"].AsInt32)).ToArray())).ToArray();

    private static BsonValue Nullable(double? value) => value is null ? BsonNull.Value : new BsonDouble(value.Value);

    private static double? Number(BsonValue value) => value.IsBsonNull ? null : value.ToDouble();

    /// <summary>A kind from the fixed set its enum names, parsed once where it enters (ADR 0024).</summary>
    private static BenchmarkGraderKind Kind(BsonValue value) =>
        SettingChoice.TryParse(value.AsString, out BenchmarkGraderKind? kind) && kind is { } known
            ? known
            : throw new FormatException(value.AsString);
}
