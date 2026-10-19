using System.Text.Json;

namespace LouisAgent.Api.Benchmarks.Runs;

/// <summary>
/// The line protocol between a check's script and the API (F31): the script prints plain lines for people and, for the
/// portal, <c>@</c> followed by one JSON object with an <c>event</c> (<c>protocol</c>, <c>mode</c>, <c>question</c>,
/// <c>sse</c>, <c>turn</c>, <c>conversation</c>, <c>mode_done</c>). Every event but <c>protocol</c> carries its mode's
/// <c>repeat</c>; a question and a turn carry their <c>session</c>, so the ledger's rows join to them (step 4). Costs are
/// never in it: they come from the ledger. Anything that isn't a whole, known event is a <see cref="LogLine"/>, never an
/// exception, so a run never fails on its own output.
/// </summary>
public static class BenchmarkProtocol
{
    /// <summary>The version this API reads; the script says its own in its first line.</summary>
    public const int Version = 1;

    public abstract record Event;

    /// <summary>A line for people, or one the protocol couldn't read: kept for the run's log.</summary>
    public sealed record LogLine(string Text) : Event;

    public sealed record ProtocolStarted(int Version, DateTimeOffset? At = null) : Event;

    /// <summary>A mode begins one repeat: what its sessions open with.</summary>
    public sealed record ModeStarted(string Mode, int Repeat, IReadOnlyList<string> Toolsets, string? Role, string? Llm,
        int? Conversations, DateTimeOffset? At = null) : Event;

    public sealed record QuestionAsked(TurnKey Key, string Session, string Text, DateTimeOffset? At = null) : Event;

    /// <summary>One event of the turn's stream, as the session sent it; <see cref="Data"/> outlives the line.</summary>
    public sealed record SseEvent(TurnKey Key, string Type, JsonElement Data, DateTimeOffset? At = null) : Event;

    public sealed record TurnEnded(TurnKey Key, string Session, string Question, string? Answer, string? Role, string? Continued,
        int PulsCalls, int SpecialistCalls, IReadOnlyList<string> Hosts, double Seconds, string? Error, DateTimeOffset? At = null) : Event;

    /// <summary>A conversation's result; <see cref="Scores"/> is the check's own document (each check scores its own way).</summary>
    public sealed record ConversationEnded(ConversationKey Key, string? Role, JsonElement Scores, int Errors, double Seconds,
        DateTimeOffset? At = null) : Event;

    /// <summary>A mode's repeat is done: a marker only.</summary>
    public sealed record ModeEnded(string Mode, int Repeat, DateTimeOffset? At = null) : Event;

    public sealed record ConversationKey(string Mode, int Repeat, string Task);

    public sealed record TurnKey(string Mode, int Repeat, string Task, int Turn)
    {
        public ConversationKey Conversation => new(Mode, Repeat, Task);
    }

    /// <summary>One line of the script's output as an event; a <see cref="LogLine"/> for anything else.</summary>
    public static Event Parse(string line)
    {
        if (!line.StartsWith('@')) return new LogLine(line);
        try
        {
            using var document = JsonDocument.Parse(line.AsMemory(1));
            var e = document.RootElement;
            if (e.ValueKind != JsonValueKind.Object) return new LogLine(line);
            var at = Time(e);
            return Text(e, "event") switch
            {
                "protocol" => new ProtocolStarted(Int(e, "version"), at),
                "mode" => new ModeStarted(Required(e, "mode"), Int(e, "repeat"), Strings(e, "toolsets"),
                    Text(e, "role") ?? Text(e, "personality"),   // CL14: a harness before CL14 said personality
                    Text(e, "llm"), OptionalInt(e, "conversations"), at),
                "question" => new QuestionAsked(Turn(e), Required(e, "session"), Required(e, "text"), at),
                "sse" => new SseEvent(Turn(e), Required(e, "type"), Json(e, "data"), at),
                "turn" => new TurnEnded(Turn(e), Required(e, "session"), Required(e, "question"), Text(e, "answer"), Text(e, "role"),
                    Text(e, "continued"), OptionalInt(e, "puls_calls") ?? 0, OptionalInt(e, "specialist_calls") ?? 0,
                    Strings(e, "hosts"), OptionalNumber(e, "seconds") ?? 0, Text(e, "error"), at),
                "conversation" => new ConversationEnded(new ConversationKey(Required(e, "mode"), Int(e, "repeat"), Required(e, "task")),
                    Text(e, "role"), Json(e, "scores"), OptionalInt(e, "errors") ?? 0, OptionalNumber(e, "seconds") ?? 0, at),
                "mode_done" => new ModeEnded(Required(e, "mode"), Int(e, "repeat"), at),
                _ => new LogLine(line),
            };
        }
        catch (Exception ex) when (ex is JsonException or MissingField or InvalidOperationException or FormatException)
        {
            return new LogLine(line);
        }
    }

    private static TurnKey Turn(JsonElement e) => new(Required(e, "mode"), Int(e, "repeat"), Required(e, "task"), Int(e, "turn"));

    /// <summary>A field the event can't be read without is missing or of the wrong type.</summary>
    private sealed class MissingField : Exception;

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string Required(JsonElement e, string name) => Text(e, name) ?? throw new MissingField();

    private static int Int(JsonElement e, string name) => OptionalInt(e, name) ?? throw new MissingField();

    private static int? OptionalInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int n) ? n : null;

    private static double? OptionalNumber(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    private static IReadOnlyList<string> Strings(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()!).ToArray()
            : [];

    /// <summary>A copy of the field that outlives the line's document; an empty object when it's missing.</summary>
    private static JsonElement Json(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) ? value.Clone() : EmptyObject;

    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>The script's <c>t</c>: Unix seconds, to the millisecond.</summary>
    private static DateTimeOffset? Time(JsonElement e) =>
        OptionalNumber(e, "t") is { } seconds ? DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Round(seconds * 1000)) : null;
}
