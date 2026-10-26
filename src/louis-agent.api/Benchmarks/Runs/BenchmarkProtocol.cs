using System.Text.Json;
using LouisAgent.Core.Config;

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

    /// <summary>
    /// The run's first line: the protocol's version, and (F18 step 2a) the eval it runs, its version, the commit it was
    /// built from, its graders and the model each level ran on. A line from a harness without them reads them as null.
    /// </summary>
    public sealed record ProtocolStarted(int Version, DateTimeOffset? At = null, string? Eval = null, int? EvalVersion = null,
        string? Commit = null, IReadOnlyList<GraderInfo>? Graders = null, IReadOnlyList<ModelInfo>? Models = null,
        BenchmarkRunSplit? Split = null, string? RescoreOf = null) : Event;

    /// <summary>One of the eval's graders: its name, its version and its kind ("FixedRule", "Model", "Human").</summary>
    public sealed record GraderInfo(string Name, int Version, BenchmarkGraderKind Kind);

    /// <summary>The model one level of the run ran on: its mode, its level (agent, specialist, minion, grader) and the model.</summary>
    public sealed record ModelInfo(string Mode, BenchmarkModelLevel Level, string? Role, string? Model);

    /// <summary>One grader's verdict on a conversation: its score, its outcome ("Pass", "Fail", "Unknown") and the reason.</summary>
    public sealed record GradeInfo(string Grader, int Version, double Score, BenchmarkOutcome Outcome, string Reason);

    /// <summary>A mode begins one repeat: what its sessions open with.</summary>
    public sealed record ModeStarted(string Mode, int Repeat, IReadOnlyList<string> Toolsets, string? Role, string? Llm,
        int? Conversations, DateTimeOffset? At = null) : Event;

    public sealed record QuestionAsked(TurnKey Key, string Session, string Text, DateTimeOffset? At = null) : Event;

    /// <summary>One event of the turn's stream, as the session sent it; <see cref="Data"/> outlives the line.</summary>
    public sealed record SseEvent(TurnKey Key, string Type, JsonElement Data, DateTimeOffset? At = null) : Event;

    public sealed record TurnEnded(TurnKey Key, string Session, string Question, string? Answer, string? Role, string? Continued,
        int PulsCalls, int SpecialistCalls, IReadOnlyList<string> Hosts, double Seconds, string? Error, DateTimeOffset? At = null,
        BenchmarkTurnFailure? Failure = null) : Event;

    /// <summary>A conversation's result; <see cref="Scores"/> is the check's own document (each check scores its own way).</summary>
    public sealed record ConversationEnded(ConversationKey Key, string? Role, JsonElement Scores, int Errors, double Seconds,
        DateTimeOffset? At = null, IReadOnlyList<GradeInfo>? Grades = null, BenchmarkOutcome? Outcome = null) : Event;

    /// <summary>A mode's repeat is done; its rates when the runner wrote them (F18 step 5a), null on a line from before.</summary>
    public sealed record ModeEnded(string Mode, int Repeat, DateTimeOffset? At = null, RatesInfo? Rates = null) : Event;

    /// <summary>The rates on a <c>mode_done</c> line: pass@k and pass^k (left out when no trial counted), k, and the trials left out or retried.</summary>
    public sealed record RatesInfo(double? PassAtK, double? PassHatK, int K, int Infrastructure, int Retried);

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
                "protocol" => new ProtocolStarted(Int(e, "version"), at, Text(e, "eval"), OptionalInt(e, "eval_version"), Text(e, "commit"),
                    Graders(e), Models(e), Split(e), Text(e, "rescore_of")),
                "mode" => new ModeStarted(Required(e, "mode"), Int(e, "repeat"), Strings(e, "toolsets"),
                    Text(e, "role") ?? Text(e, "personality"),   // CL14: a harness before CL14 said personality
                    Text(e, "llm"), OptionalInt(e, "conversations"), at),
                "question" => new QuestionAsked(Turn(e), Required(e, "session"), Required(e, "text"), at),
                "sse" => new SseEvent(Turn(e), Required(e, "type"), Json(e, "data"), at),
                "turn" => new TurnEnded(Turn(e), Required(e, "session"), Required(e, "question"), Text(e, "answer"), Text(e, "role"),
                    Text(e, "continued"), OptionalInt(e, "puls_calls") ?? 0, OptionalInt(e, "specialist_calls") ?? 0,
                    Strings(e, "hosts"), OptionalNumber(e, "seconds") ?? 0, Text(e, "error"), at, Failure(e)),
                "conversation" => new ConversationEnded(new ConversationKey(Required(e, "mode"), Int(e, "repeat"), Required(e, "task")),
                    Text(e, "role"), Json(e, "scores"), OptionalInt(e, "errors") ?? 0, OptionalNumber(e, "seconds") ?? 0, at,
                    Grades(e), Outcome(e, "outcome")),
                "mode_done" => new ModeEnded(Required(e, "mode"), Int(e, "repeat"), at, Rates(e)),
                "summary" => SummaryLineReader.Of(e, at) is { } summary ? summary : new LogLine(line),
                _ => new LogLine(line),
            };
        }
        catch (Exception ex) when (ex is JsonException or MissingField or InvalidOperationException or FormatException)
        {
            return new LogLine(line);
        }
    }

    /// <summary>The scenarios the run asked for (F18 step 5.4), by their snake-case name; absent is none, an unknown name an unreadable line.</summary>
    private static BenchmarkRunSplit? Split(JsonElement e) =>
        Text(e, "split") is null ? null : Choice(e, "split", BenchmarkRunSplitName.Aliases);

    /// <summary>One slice of a mode's roll-up (F18 step 5c), written after the mode's last mode_done.</summary>
    public sealed record SummaryReported(BenchmarkSlice Slice, DateTimeOffset? At = null) : Event;

    /// <summary>The eval's graders, from the protocol line's <c>graders</c>; null when the line doesn't name any.</summary>
    private static IReadOnlyList<GraderInfo>? Graders(JsonElement e) =>
        e.TryGetProperty("graders", out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(g => new GraderInfo(Required(g, "name"), Int(g, "version"), Choice<BenchmarkGraderKind>(g, "kind"))).ToArray()
            : null;

    /// <summary>The model each level ran on, from the protocol line's <c>models</c>; null when the line doesn't name any.</summary>
    private static IReadOnlyList<ModelInfo>? Models(JsonElement e) =>
        e.TryGetProperty("models", out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(m => new ModelInfo(Required(m, "mode"), Choice<BenchmarkModelLevel>(m, "level"), Text(m, "role"), Text(m, "model"))).ToArray()
            : null;

    /// <summary>The grades of a conversation, from its <c>grades</c>; null when the line doesn't carry any.</summary>
    private static IReadOnlyList<GradeInfo>? Grades(JsonElement e) =>
        e.TryGetProperty("grades", out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(g => new GradeInfo(Required(g, "grader"), Int(g, "version"),
                OptionalNumber(g, "score") ?? throw new MissingField(), Choice<BenchmarkOutcome>(g, "outcome"), Text(g, "reason") ?? "")).ToArray()
            : null;

    /// <summary>An outcome that is absent is null; one that names no outcome is a line the protocol can't read.</summary>
    private static BenchmarkOutcome? Outcome(JsonElement e, string name) =>
        Text(e, name) is null ? null : Choice<BenchmarkOutcome>(e, name);

    /// <summary>A choice from the fixed set its enum names, parsed once where the line enters (ADR 0024); a name that isn't one is a line the protocol can't read.</summary>
    private static T Choice<T>(JsonElement e, string name, IReadOnlyDictionary<string, T>? aliases = null) where T : struct, Enum =>
        SettingChoice.TryParse(Text(e, name), out T? choice, aliases) && choice is { } value ? value : throw new MissingField();

    /// <summary>The infrastructure failure that ended a turn (F18 step 5.1), by its snake-case name; absent is none, an unknown name an unreadable line.</summary>
    private static BenchmarkTurnFailure? Failure(JsonElement e) =>
        Text(e, "failure") is null ? null : Choice(e, "failure", BenchmarkTurnFailureName.Aliases);

    private static TurnKey Turn(JsonElement e) => new(Required(e, "mode"), Int(e, "repeat"), Required(e, "task"), Int(e, "turn"));

    /// <summary>A field the event can't be read without is missing or of the wrong type.</summary>
    private sealed class MissingField : Exception;

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static string Required(JsonElement e, string name) => Text(e, name) ?? throw new MissingField();

    /// <summary>The rates on a mode_done line (F18 step 5a): there when it names its k, null on a line from before; a rate left out is null, never 0.</summary>
    private static RatesInfo? Rates(JsonElement e) =>
        OptionalInt(e, "k") is { } k
            ? new RatesInfo(OptionalNumber(e, "pass_at_k"), OptionalNumber(e, "pass_hat_k"), k, OptionalInt(e, "infrastructure") ?? 0, OptionalInt(e, "retried") ?? 0)
            : null;

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
