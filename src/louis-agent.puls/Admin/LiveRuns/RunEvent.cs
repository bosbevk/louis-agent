using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Choices;

namespace LouisAgent.Puls.Admin.LiveRuns;

/// <summary>
/// One event the run's script writes on a line of its own, after an <c>@</c>: a question, a piece of a turn's stream,
/// a turn that ended, or a conversation's scores. Only the fields its kind has are set.
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA), read once where the line comes in. The script's protocol has no record in the
/// contract (the eval runner writes it byte by byte), so this is the page's reading of it: the page then works with
/// names the compiler checks instead of picking the line apart by name in every place that needs a field.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public sealed record RunEvent(
    string? Event,
    string? Mode,
    int? Repeat,
    string? Task,
    int? Turn,
    string? Text,
    string? Type,
    JsonElement? Data,
    string? Role,
    string? Continued,
    string? Error,
    int? PulsCalls,
    int? SpecialistCalls,
    double? Seconds,
    JsonElement? Scores)
{
    /// <summary>What the event is about; none for a kind this page doesn't show (the table leaves it alone).</summary>
    public RunEventKind? Kind => SettingChoice.TryParse<RunEventKind>(Event, out var kind) ? kind : null;

    /// <summary>Which turn the event is about; a line that names no repeat or turn means the first.</summary>
    public TurnKey Key => new(Mode ?? "", Repeat ?? 1, Task ?? "", Turn ?? 1);

    /// <summary>
    /// Reads an event line (without its <c>@</c>). A line that isn't a JSON object isn't an event, and the page shows
    /// it with the script's plain lines instead.
    /// </summary>
    public static RunEvent? TryRead(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            // Only an object is an event; an array or a bare value is just text that happens to start with @.
            if (document.RootElement.ValueKind != JsonValueKind.Object) return null;
            return document.RootElement.Deserialize<RunEvent>(AdminJson.SnakeCase);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
