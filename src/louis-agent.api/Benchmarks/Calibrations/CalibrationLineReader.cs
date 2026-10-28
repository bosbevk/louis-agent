using System.Text.Json;

namespace LouisAgent.Api.Benchmarks.Calibrations;

/// <summary>
/// Reads the runner's <c>calibration</c> line, the report of a judge check, into a record we can keep. Any other line,
/// or one it can't read, gives nothing, and then the recorder stores nothing.
/// </summary>
/// <remarks>
/// It reads by hand with <see cref="JsonElement"/>, as the protocol reader does: the runner is another process, so its
/// line is input the API doesn't own, and a missing field becomes empty instead of failing the whole line.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class CalibrationLineReader
{
    /// <summary>The judge check this line reports, or nothing when it isn't one.</summary>
    public static CalibrationRecord? Read(string line, string run, DateTimeOffset at)
    {
        if (!line.StartsWith('@')) return null;
        try
        {
            // The JSON starts after the @.
            using var document = JsonDocument.Parse(line.AsMemory(1));
            var e = document.RootElement;
            if (e.ValueKind != JsonValueKind.Object || Text(e, "event") != "calibration") return null;
            return new CalibrationRecord(run, Text(e, "grader") ?? "", Text(e, "judge_model"), at, Int(e, "agreed"),
                Int(e, "total"), Int(e, "bar"),
                e.TryGetProperty("passes", out var passes) && passes.ValueKind == JsonValueKind.True,
                Class(e, "person_pass"), Class(e, "person_fail"), Disagreements(e));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>What the judge said to one class of the people's labels. Missing counts are zeros.</summary>
    private static CalibrationClassCounts Class(JsonElement e, string name) =>
        e.TryGetProperty(name, out var c) && c.ValueKind == JsonValueKind.Object
            ? new CalibrationClassCounts(Int(c, "judge_pass"), Int(c, "judge_fail"), Int(c, "judge_unknown"))
            : new CalibrationClassCounts(0, 0, 0);

    /// <summary>Every conversation where the judge and the person differed. No list means none.</summary>
    private static IReadOnlyList<CalibrationDisagreement> Disagreements(JsonElement e) =>
        e.TryGetProperty("disagreements", out var list) && list.ValueKind == JsonValueKind.Array
            ?
            [
                .. list.EnumerateArray().Select(d => new CalibrationDisagreement(Text(d, "scenario") ?? "",
                    Text(d, "mode") ?? "", Int(d, "repeat"), Text(d, "label") ?? "", Text(d, "judge") ?? "",
                    Text(d, "reason") ?? ""))
            ]
            : [];

    /// <summary>A text field, if it's there and really is text.</summary>
    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>A whole-number field, zero when it's missing.</summary>
    private static int Int(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
}
