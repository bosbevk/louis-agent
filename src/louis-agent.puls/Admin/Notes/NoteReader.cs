using System.Text.Json;
using LouisAgent.Puls.Admin.Reviews;

namespace LouisAgent.Puls.Admin.Notes;

/// <summary>
/// Pulls the notes out of the run the API sent, so no component has to dig in the JSON.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class NoteReader
{
    /// <summary>The run's notes in the order written. A run from an older API has none.</summary>
    public static IReadOnlyList<RunNoteEntry> Of(JsonElement run)
    {
        if (!run.TryGetProperty("notes", out var list) || list.ValueKind != JsonValueKind.Array) return [];
        string runId = run.GetProperty("run").GetProperty("id").GetString()!;
        return list.EnumerateArray().Select(note => Entry(runId, note)).ToArray();
    }

    /// <summary>One note from its JSON.</summary>
    private static RunNoteEntry Entry(string runId, JsonElement note) =>
        new(note.GetProperty("text").GetString()!, note.GetProperty("at").GetDateTimeOffset(),
            note.GetProperty("conversation") is { ValueKind: JsonValueKind.Object } key
                ? new ConversationRef(runId, key.GetProperty("mode").GetString()!, key.GetProperty("repeat").GetInt32(), key.GetProperty("task").GetString()!)
                : null);
}
