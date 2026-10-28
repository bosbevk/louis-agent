using System.Text.Json;
using LouisAgent.Api.Evals.Runs.Protocol;
using LouisAgent.Choices;

namespace LouisAgent.Api.Evals.Export;

/// <summary>
/// Writes a stored slice back as the runner's <c>summary</c> line: its fields as the runner wrote them, in its order,
/// and nothing else. A rate the slice had too few trials for is left out.
/// </summary>
/// <remarks>
/// There's no cost in it on purpose: the ledger prices a slice when it is read, so an export never carries a cost, and
/// scoring an export again can't carry an old one into a new run.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
internal static class SummaryExport
{
    /// <summary>Writes the slice's summary line: its counts, rates, and its grader, kind and score lists.</summary>
    public static void Write(ProtocolLines lines, EvalSlice slice) => lines.Line("summary", json =>
    {
        json.WriteString("mode", slice.Mode);
        json.WriteString("slice", slice.Slice);
        json.WriteNumber("n", slice.N);
        json.WriteNumber("passed", slice.Passed);
        json.WriteNumber("failed", slice.Failed);
        json.WriteNumber("unknown", slice.Unknown);
        json.WriteNumber("infrastructure", slice.Infrastructure);
        json.WriteStartArray("tasks");
        foreach (string task in slice.Tasks) json.WriteStringValue(task);
        json.WriteEndArray();
        json.WriteBoolean("too_few", slice.TooFew);
        if (slice.PassRate is { } passRate) json.WriteNumber("pass_rate", passRate);
        if (slice.PassAtK is { } passAtK) json.WriteNumber("pass_at_k", passAtK);
        if (slice.PassHatK is { } passHatK) json.WriteNumber("pass_hat_k", passHatK);
        Array(json, "graders", slice.Graders, (g, o) =>
        {
            o.WriteString("grader", g.Grader);
            o.WriteString("kind", g.Kind.Wire());
            Counts(o, g.Passed, g.Failed, g.Unknown);
        });
        Array(json, "kinds", slice.Kinds, (k, o) =>
        {
            o.WriteString("kind", k.Kind.Wire());
            Counts(o, k.Passed, k.Failed, k.Unknown);
        });
        Array(json, "scores", slice.Scores, (s, o) =>
        {
            o.WriteString("key", s.Key);
            o.WriteNumber("mean", s.Mean);
            o.WriteNumber("n", s.N);
        });
    });

    /// <summary>A named list with one JSON object per item, each filled in by the given writer.</summary>
    private static void Array<T>(Utf8JsonWriter json, string name, IReadOnlyList<T> items,
        Action<T, Utf8JsonWriter> write)
    {
        json.WriteStartArray(name);
        foreach (var item in items)
        {
            json.WriteStartObject();
            write(item, json);
            json.WriteEndObject();
        }

        json.WriteEndArray();
    }

    /// <summary>How many passed, failed and couldn't be told.</summary>
    private static void Counts(Utf8JsonWriter json, int passed, int failed, int unknown)
    {
        json.WriteNumber("passed", passed);
        json.WriteNumber("failed", failed);
        json.WriteNumber("unknown", unknown);
    }
}
