using System.Text.Json;
using System.Text.Json.Serialization;
using LouisAgent.Composition.Messages;
using LouisAgent.Packs.Triage.Messages;
using LouisAgent.Packs.Triage.Runs.Steps;

namespace LouisAgent.Packs.Triage.Runs;

/// <summary>
/// Keeps each run as one JSON-lines file under one folder, a step a line as it lands, so a run can be read while it
/// goes and after a restart, by this process or by a person with a text editor.
/// </summary>
/// <remarks>
/// The first <see cref="ITriageRecorder"/>; F106.3.3 adds one on the saved conversations' store (F43). A run id is a
/// file name, so one that isn't a plain token is refused before any path is built (ADR 0004).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public sealed class FileTriageRecorder(string root) : ITriageRecorder
{
    /// <summary>Enums by their wire names, so another reader understands the file.</summary>
    internal static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };

    /// <inheritdoc />
    public async Task SaveAsync(TriageStep step, CancellationToken cancellationToken = default)
    {
        string path = PathOf(step.RunId);
        Directory.CreateDirectory(root);
        await File.AppendAllTextAsync(path, JsonSerializer.Serialize(step, Json) + "\n", cancellationToken);
    }

    /// <inheritdoc />
    public async Task<TriageRun?> ReadAsync(string runId, CancellationToken cancellationToken = default)
    {
        string path = PathOf(runId);
        if (!File.Exists(path)) return null;

        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        var steps = lines.Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => JsonSerializer.Deserialize<TriageStep>(line, Json)!);
        return TriageRun.Of(runId, steps);
    }

    /// <summary>The run's file, once its id is known to be a plain token.</summary>
    private string PathOf(string runId)
    {
        if (!PlainId.Is(runId))
            throw new ArgumentException(MessageText.Format(TriageToolMessage.BadRunId, new { RunId = runId }),
                nameof(runId));
        return Path.Combine(root, runId + ".jsonl");
    }
}
