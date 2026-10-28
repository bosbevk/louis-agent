using System.Text.Json;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Choices;

namespace LouisAgent.Api.Evals.Runs.Protocol;

/// <summary>
/// Reads the runner's <c>summary</c> line into its slice, keeping the runner's numbers as they are and working out
/// none of them. A rate the runner left out (too few trials) stays null, never 0.
/// </summary>
/// <remarks>
/// Tolerant Reader: a line missing a field it needs, or naming a grader kind this build doesn't know, isn't a summary
/// we can read. <see cref="Of"/> then gives nothing back and the caller keeps it as a log line, like any line it can't
/// read, so the run carries on.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal static class SummaryLineReader
{
    /// <summary>The slice a summary line describes, or nothing if the line is missing something it needs.</summary>
    public static EvalProtocol.SummaryReported? Of(JsonElement e, DateTimeOffset? at)
    {
        // Every field the slice can't do without must be there and the right type, or this isn't a summary we read.
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

        // The tasks are optional: anything that isn't text is skipped.
        var tasks = e.TryGetProperty("tasks", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Where(t => t.ValueKind == JsonValueKind.String).Select(t => t.GetString()!)
                .ToArray()
            : [];

        return new EvalProtocol.SummaryReported(new EvalSlice(mode, slice, n, passed, failed, unknown,
            infrastructure, tasks, tooFew.GetBoolean(),
            Number(e, "pass_rate"), Number(e, "pass_at_k"), Number(e, "pass_hat_k"), graders, kinds, scores), at);
    }

    /// <summary>Each grader's counts, or nothing if any of them can't be read.</summary>
    private static IReadOnlyList<SliceGrader>? Graders(JsonElement e) => Objects(e, "graders", g =>
        Text(g, "grader") is { } name && Kind(g) is { } kind && Counts(g) is var (passed, failed, unknown)
            ? new SliceGrader(name, kind, passed, failed, unknown)
            : null);

    /// <summary>The counts per grader kind, or nothing if any of them can't be read.</summary>
    private static IReadOnlyList<SliceKind>? Kinds(JsonElement e) => Objects(e, "kinds", k =>
        Kind(k) is { } kind && Counts(k) is var (passed, failed, unknown)
            ? new SliceKind(kind, passed, failed, unknown)
            : null);

    /// <summary>The average of each numeric score, or nothing if any of them can't be read.</summary>
    private static IReadOnlyList<SliceScore>? Scores(JsonElement e) => Objects(e, "scores", s =>
        Text(s, "key") is { } key && Number(s, "mean") is { } mean && Int(s, "n") is { } n
            ? new SliceScore(key, mean, n)
            : null);

    /// <summary>
    /// Reads every object in a list the same way. If the list is missing, or even one item can't be read, you get
    /// nothing: half a list would give wrong totals.
    /// </summary>
    private static IReadOnlyList<T>? Objects<T>(JsonElement e, string name, Func<JsonElement, T?> read) where T : class
    {
        if (!e.TryGetProperty(name, out var array) || array.ValueKind != JsonValueKind.Array) return null;

        var items = array.EnumerateArray().Select(read).ToList();
        return items.Any(item => item is null) ? null : items!;
    }

    /// <summary>The grader kind, if it's one we know.</summary>
    private static EvalGraderKind? Kind(JsonElement e) =>
        SettingChoice.TryParse(Text(e, "kind"), out EvalGraderKind? kind) ? kind : null;

    /// <summary>Passed, failed and unknown, if all three are there.</summary>
    private static (int Passed, int Failed, int Unknown)? Counts(JsonElement e) =>
        Int(e, "passed") is { } passed && Int(e, "failed") is { } failed && Int(e, "unknown") is { } unknown
            ? (passed, failed, unknown)
            : null;

    /// <summary>A text field, if it's there and is text.</summary>
    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>A whole number, if it's there and fits.</summary>
    private static int? Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int n)
            ? n
            : null;

    /// <summary>A number, if it's there.</summary>
    private static double? Number(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;
}
