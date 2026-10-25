using System.Text.Json;

namespace LouisAgent.Puls.Admin.Grades;

/// <summary>Reads a run's eval and graders from <c>GET /admin/benchmarks/runs/{id}</c>'s answer (its <c>run</c> object); null where the run recorded none.</summary>
public static class RunGradersReader
{
    public static RunGraders Read(JsonElement response)
    {
        if (!response.TryGetProperty("run", out var run) || run.ValueKind != JsonValueKind.Object) return new RunGraders(null, null, null, null);
        IReadOnlyList<GraderRow>? graders = run.TryGetProperty("graders", out var list) && list.ValueKind == JsonValueKind.Array
            ? list.EnumerateArray().Select(g => new GraderRow(Text(g, "name") ?? "", Int(g, "version") ?? 0, Text(g, "kind") ?? "")).ToList()
            : null;
        return new RunGraders(Text(run, "eval"), Int(run, "eval_version"), Text(run, "commit"), graders);
    }

    private static string? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static int? Int(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetInt32(out int n) ? n : null;
}
