using System.Text.Json;
using LouisAgent.Choices;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// Reads the runner's <c>summary</c> line (F18 step 5c.2) into its slice. The API keeps the runner's fields as they are and computes none of them. A line missing a required field,
/// or naming a kind this build doesn't know, is not a summary this API can read: <see cref="Of"/> gives null and the caller keeps it as a log line, as it does any line it can't read.
/// A rate left out (a slice with too few trials) is null, never 0.
/// </summary>
internal static class SummaryLineReader
{
    public static BenchmarkProtocol.SummaryReported? Of(JsonElement e, DateTimeOffset? at)
    {
        if (Text(e, "mode") is not { } mode || Text(e, "slice") is not { } slice
                                            || Int(e, "n") is not { } n || Int(e, "passed") is not { } passed ||
                                            Int(e, "failed") is not { } failed
                                            || Int(e, "unknown") is not { } unknown || Int(e, "infrastructure") is not
                                            { } infrastructure
                                            || !e.TryGetProperty("too_few", out var tooFew) ||
                                            tooFew.ValueKind is not (JsonValueKind.True or JsonValueKind.False)
                                            || Graders(e) is not { } graders || Kinds(e) is not { } kinds ||
                                            Scores(e) is not { } scores)
            return null;

        var tasks = e.TryGetProperty("tasks", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!)
                .ToArray()
            : [];
        return new BenchmarkProtocol.SummaryReported(new BenchmarkSlice(mode, slice, n, passed, failed, unknown,
            infrastructure, tasks, tooFew.GetBoolean(),
            Number(e, "pass_rate"), Number(e, "pass_at_k"), Number(e, "pass_hat_k"), graders, kinds, scores), at);
    }

    private static IReadOnlyList<SliceGrader>? Graders(JsonElement e) => Objects(e, "graders", g =>
        Text(g, "grader") is { } name && Kind(g) is { } kind && Counts(g) is var (passed, failed, unknown)
            ? new SliceGrader(name, kind, passed, failed, unknown)
            : null);

    private static IReadOnlyList<SliceKind>? Kinds(JsonElement e) => Objects(e, "kinds", k =>
        Kind(k) is { } kind && Counts(k) is var (passed, failed, unknown)
            ? new SliceKind(kind, passed, failed, unknown)
            : null);

    private static IReadOnlyList<SliceScore>? Scores(JsonElement e) => Objects(e, "scores", s =>
        Text(s, "key") is { } key && Number(s, "mean") is { } mean && Int(s, "n") is { } n
            ? new SliceScore(key, mean, n)
            : null);

    /// <summary>The objects of an array property, each read by <paramref name="read"/>; null when the property is missing or any element can't be read.</summary>
    private static IReadOnlyList<T>? Objects<T>(JsonElement e, string name, Func<JsonElement, T?> read) where T : class
    {
        if (!e.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array) return null;
        var items = array.EnumerateArray().Select(read).ToList();
        return items.Any(item => item is null) ? null : items!;
    }

    private static BenchmarkGraderKind? Kind(JsonElement e) =>
        SettingChoice.TryParse(Text(e, "kind"), out BenchmarkGraderKind? kind) ? kind : null;

    private static (int Passed, int Failed, int Unknown)? Counts(JsonElement e) =>
        Int(e, "passed") is { } passed && Int(e, "failed") is { } failed && Int(e, "unknown") is { } unknown
            ? (passed, failed, unknown)
            : null;

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static int? Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int n)
            ? n
            : null;

    private static double? Number(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
}
