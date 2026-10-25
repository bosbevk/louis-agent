using System.Text.Json;

namespace LouisAgent.Puls.Admin.Rates;

/// <summary>
/// Reads each mode's rates from the run's JSON (<c>GET /v1/admin/benchmarks/runs/{id}</c>): a standard record, not an eval's score key. The runner wrote
/// them on every repeat's line, over the repeats so far, so a mode's are the last repeat's that has them; a mode from before the rates has none.
/// </summary>
public static class ModeRatesReader
{
    public static IReadOnlyList<ModeRate> Read(JsonElement run)
    {
        if (!run.TryGetProperty("modes", out var modes) || modes.ValueKind != JsonValueKind.Array) return [];

        var rates = new List<ModeRate>();
        foreach (var mode in modes.EnumerateArray().GroupBy(m => m.GetProperty("name").GetString()!))
        {
            var last = mode.Where(m => Int(m, "k") is not null).OrderBy(m => m.GetProperty("repeat").GetInt32()).LastOrDefault();
            rates.Add(last.ValueKind == JsonValueKind.Undefined
                ? new ModeRate(mode.Key, null, null, null, 0, 0)
                : new ModeRate(mode.Key, Int(last, "k"), Number(last, "pass_at_k"), Number(last, "pass_hat_k"), Int(last, "infrastructure") ?? 0, Int(last, "retried") ?? 0));
        }
        return rates;
    }

    private static int? Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;

    private static double? Number(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
}
