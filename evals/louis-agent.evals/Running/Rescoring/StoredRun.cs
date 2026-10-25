using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Messages;
using LouisAgent.Evals.Protocol;

namespace LouisAgent.Evals.Running.Rescoring;

/// <summary>One line of a stored run as it was written, and its JSON (F18 step 5b). A line that isn't an <c>@</c> line is no part of the protocol.</summary>
internal sealed record StoredLine(string Raw, string Kind, JsonElement Json)
{
    public static StoredLine? Parse(string raw)
    {
        if (!raw.StartsWith('@')) return null;
        using var document = JsonDocument.Parse(raw[1..]);
        var json = document.RootElement.Clone();
        return new StoredLine(raw, json.GetProperty("event").GetString() ?? string.Empty, json);
    }

    public string Mode => Json.GetProperty("mode").GetString()!;

    public int Repeat => Json.GetProperty("repeat").GetInt32();

    public string Task => Json.GetProperty("task").GetString()!;

    /// <summary>What the same turn or conversation is called in the lines of a run: its mode, repeat and task.</summary>
    public (string Mode, int Repeat, string Task) Key => (Mode, Repeat, Task);

    public string? Text(string name) => Json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    public double? Decimal(string name) => Json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    public int? Number(string name) => Json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;
}

/// <summary>
/// A stored run's protocol lines, read for a re-score (F18 step 5b): what its first line says it ran (the split and the model of each mode), and
/// the mode each <c>mode</c> line named, which is the mode the graders see. The lines are only read, never changed.
/// </summary>
internal sealed class StoredRun
{
    private StoredRun(IReadOnlyList<StoredLine> lines, StoredLine begin)
    {
        Lines = lines;
        Split = SplitSelections.FromWireName(begin.Text("split"));
        Models = begin.Json.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array
            ? [.. models.EnumerateArray().Select(ModelOf)]
            : [];
    }

    public IReadOnlyList<StoredLine> Lines { get; }

    public SplitSelection Split { get; }

    public IReadOnlyList<ModelUse> Models { get; }

    public static StoredRun Read(IEnumerable<string> raw)
    {
        var lines = raw.Select(StoredLine.Parse).OfType<StoredLine>().ToList();
        // The old first line was the version alone; a run that can be re-scored names its eval.
        var begin = lines.FirstOrDefault(line => line.Kind == "protocol" && line.Json.TryGetProperty("eval", out _))
                    ?? throw new InvalidDataException(MessageText.Format(EvalErrorMessage.NoBeginLine));
        return new StoredRun(lines, begin);
    }

    /// <summary>The mode a <c>mode</c> line named, as the run opened it.</summary>
    public static EvalMode ModeOf(StoredLine line) =>
        new(line.Mode, line.Text("role") ?? string.Empty,
            [.. line.Json.GetProperty("toolsets").EnumerateArray().Select(toolset => toolset.GetString() ?? string.Empty)], Llm: line.Text("llm"));

    private static ModelUse ModelOf(JsonElement model) =>
        new(model.GetProperty("mode").GetString()!, Enum.Parse<ModelLevel>(model.GetProperty("level").GetString()!),
            model.TryGetProperty("role", out var role) ? role.GetString() : null, model.TryGetProperty("model", out var name) ? name.GetString() : null);
}
