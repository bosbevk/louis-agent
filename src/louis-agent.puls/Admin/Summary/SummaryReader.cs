using System.Text.Json;

namespace LouisAgent.Puls.Admin.Summary;

/// <summary>
/// Reads a run's roll-up from its JSON (<c>summaries</c>, F18 steps 5c.2 and 5c.3): the runner's lines with the ledger's costs, as standard records, not an eval's score keys. A run from
/// before 5c has none (null, never an empty list: the portal says so and offers the re-score). The modes keep the order their lines came in, each mode's overall line first.
/// </summary>
public static class SummaryReader
{
    public static IReadOnlyList<ModeSlices>? Read(JsonElement run)
    {
        if (!run.TryGetProperty("summaries", out var lines) || lines.ValueKind != JsonValueKind.Array) return null;

        return lines.EnumerateArray().Select(SliceOf).GroupBy(slice => slice.Mode).Select(mode => new ModeSlices(mode.Key, [.. mode])).ToList();
    }

    private static SliceView SliceOf(JsonElement e) =>
        new(Text(e, "mode") ?? "", Text(e, "slice") ?? "", Int(e, "n"), Int(e, "passed"), Int(e, "failed"), Int(e, "unknown"), Int(e, "infrastructure"),
            e.TryGetProperty("tasks", out var tasks) && tasks.ValueKind == JsonValueKind.Array ? [.. tasks.EnumerateArray().Select(t => t.GetString() ?? "")] : [],
            e.TryGetProperty("too_few", out var tooFew) && tooFew.ValueKind == JsonValueKind.True,
            Number(e, "pass_rate"), Number(e, "pass_at_k"), Number(e, "pass_hat_k"), Decimal(e, "cost_per_trial"), Int(e, "passed") > 0, Decimal(e, "cost_per_pass"));

    private static string? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;

    private static double? Number(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;

    private static decimal? Decimal(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDecimal() : null;
}
