using System.Text.Json;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Messages;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Evals.Running.Rescoring;

/// <summary>
/// One line of a stored run as it was written, with its JSON. A line that isn't an <c>@</c> line is no part of the
/// protocol.
/// </summary>
internal sealed record StoredLine(string Raw, string Kind, JsonElement Json)
{
    /// <summary>Reads one stored line, or nothing when it is a person's line.</summary>
    public static StoredLine? Parse(string raw)
    {
        if (!raw.StartsWith('@')) return null;
        using var document = JsonDocument.Parse(raw[1..]);
        var json = document.RootElement.Clone();
        return new StoredLine(raw, json.GetProperty("event").GetString() ?? string.Empty, json);
    }

    /// <summary>The mode the line is about.</summary>
    public string Mode => Json.GetProperty("mode").GetString()!;

    /// <summary>Which repeat of the mode.</summary>
    public int Repeat => Json.GetProperty("repeat").GetInt32();

    /// <summary>The scenario the line is about.</summary>
    public string Task => Json.GetProperty("task").GetString()!;

    /// <summary>
    /// What the same turn or conversation is called in the lines of a run: its mode, repeat and task.
    /// </summary>
    public (string Mode, int Repeat, string Task) Key => (Mode, Repeat, Task);

    /// <summary>A text field, if the line has it.</summary>
    public string? Text(string name) =>
        Json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>A number with a fraction, if the line has it.</summary>
    public double? Decimal(string name) =>
        Json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    /// <summary>A whole number, if the line has it.</summary>
    public int? Number(string name) =>
        Json.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetInt32() : null;
}

/// <summary>
/// A stored run's protocol lines, read for a re-score: what its first line says it ran (the split and the model of
/// each mode), and the mode each <c>mode</c> line named, which is the mode the graders see. The lines are only read,
/// never changed.
/// </summary>
internal sealed class StoredRun
{
    /// <summary>Keeps the lines, and reads the split and the models off the first line.</summary>
    private StoredRun(IReadOnlyList<StoredLine> lines, StoredLine begin)
    {
        Lines = lines;
        // The split the first line names, its old held_out spelling too; a run from before splits were kept is dev.
        Split = SettingChoice.TryParse(begin.Text("split"), out RunSplit? split, RunSplits.Aliases) &&
                split is { } known
            ? known
            : RunSplit.Dev;
        Models = begin.Json.TryGetProperty("models", out var models) && models.ValueKind == JsonValueKind.Array
            ? [.. models.EnumerateArray().Select(ModelOf)]
            : [];
    }

    /// <summary>Every protocol line of the run, in the order it was written.</summary>
    public IReadOnlyList<StoredLine> Lines { get; }

    /// <summary>Which scenarios the run asked for.</summary>
    public RunSplit Split { get; }

    /// <summary>The model each part of the run ran on.</summary>
    public IReadOnlyList<ModelInfo> Models { get; }

    /// <summary>
    /// Reads a stored run's lines; a run without a first line that names its eval can't be re-scored.
    /// </summary>
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
        [
            .. line.Json.GetProperty("toolsets").EnumerateArray().Select(toolset => toolset.GetString() ?? string.Empty)
        ], Llm: line.Text("llm"));

    /// <summary>One model of the first line: its mode, level, role and name.</summary>
    private static ModelInfo ModelOf(JsonElement model) =>
        new(model.GetProperty("mode").GetString()!, Enum.Parse<ModelLevel>(model.GetProperty("level").GetString()!),
            model.TryGetProperty("role", out var role) ? role.GetString() : null,
            model.TryGetProperty("model", out var name) ? name.GetString() : null);
}
