using System.Text.Json;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Choices;

namespace LouisAgent.Api.Evals.Runs.Protocol;

/// <summary>
/// Reads what an eval's script prints while it runs. Plain lines are for people; a line that starts with <c>@</c>
/// carries one JSON event for the portal (<c>protocol</c>, <c>mode</c>, <c>question</c>, <c>sse</c>, <c>turn</c>,
/// <c>conversation</c>, <c>mode_done</c>, <c>summary</c>). Costs are never in it: they come from the ledger, which
/// joins on each question's and turn's <c>session</c>.
/// </summary>
/// <remarks>
/// Tolerant Reader. Anything that isn't a whole, known event becomes a <see cref="LogLine"/> instead of an exception,
/// so a run never fails because of its own output, and a newer script's extra fields don't break an older API.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public static class EvalProtocol
{
    /// <summary>The version this API reads; the script says its own in its first line.</summary>
    public const int Version = 1;

    /// <summary>One line of the script's output, once we've read it.</summary>
    public abstract record Event;

    /// <summary>A line for people, or one we couldn't read. It's kept for the run's log.</summary>
    public sealed record LogLine(string Text) : Event;

    /// <summary>
    /// The run's first line: which protocol version, which eval and its version, the commit it was built from, its
    /// graders and the model each level ran on. An older script leaves most of these out, so they read as null.
    /// </summary>
    public sealed record ProtocolStarted(
        int Version,
        DateTimeOffset? At = null,
        string? Eval = null,
        int? EvalVersion = null,
        string? Commit = null,
        IReadOnlyList<GraderInfo>? Graders = null,
        IReadOnlyList<ModelInfo>? Models = null,
        EvalRunSplit? Split = null,
        string? RescoreOf = null) : Event;

    /// <summary>One of the eval's graders: its name, its version and how it decides.</summary>
    public sealed record GraderInfo(string Name, int Version, EvalGraderKind Kind, string? Question = null);

    /// <summary>Which model one level of the run used, in which mode.</summary>
    public sealed record ModelInfo(string Mode, EvalModelLevel Level, string? Role, string? Model);

    /// <summary>What one grader thought of a conversation: its score, pass or fail, and why.</summary>
    public sealed record GradeInfo(
        string Grader,
        int Version,
        double Score,
        EvalOutcome Outcome,
        string Reason,
        string? JudgeModel = null,
        string? JudgePromptHash = null);

    /// <summary>A mode starts one repeat, with what its sessions open with.</summary>
    public sealed record ModeStarted(
        string Mode,
        int Repeat,
        IReadOnlyList<string> Toolsets,
        string? Role,
        string? Llm,
        int? Conversations,
        DateTimeOffset? At = null) : Event;

    /// <summary>The script asked the agent a question in a session.</summary>
    public sealed record QuestionAsked(TurnKey Key, string Session, string Text, DateTimeOffset? At = null) : Event;

    /// <summary>One event of the turn's stream, as the session sent it; <see cref="Data"/> outlives the line.</summary>
    public sealed record SseEvent(TurnKey Key, string Type, JsonElement Data, DateTimeOffset? At = null) : Event;

    /// <summary>A turn finished: the question, the answer, who answered, how long it took, what broke.</summary>
    public sealed record TurnEnded(
        TurnKey Key,
        string Session,
        string Question,
        string? Answer,
        string? Role,
        string? Continued,
        int AgentCalls,
        int SpecialistCalls,
        IReadOnlyList<string> Hosts,
        double Seconds,
        string? Error,
        DateTimeOffset? At = null,
        EvalTurnFailure? Failure = null) : Event;

    /// <summary>
    /// A conversation finished, with its grades. <see cref="Scores"/> is the eval's own document, since each eval
    /// scores in its own way.
    /// </summary>
    public sealed record ConversationEnded(
        ConversationKey Key,
        string? Role,
        JsonElement Scores,
        int Errors,
        double Seconds,
        DateTimeOffset? At = null,
        IReadOnlyList<GradeInfo>? Grades = null,
        EvalOutcome? Outcome = null) : Event;

    /// <summary>A mode's repeat is done, with its rates when the runner wrote them (an older line has none).</summary>
    public sealed record ModeEnded(string Mode, int Repeat, DateTimeOffset? At = null, RatesInfo? Rates = null) : Event;

    /// <summary>
    /// A mode's rates: pass@k and pass^k (left out when no trial counted), k, and how many trials were left out for
    /// infrastructure or retried.
    /// </summary>
    public sealed record RatesInfo(double? PassAtK, double? PassHatK, int K, int Infrastructure, int Retried);

    /// <summary>Which conversation: the mode, the repeat and the task.</summary>
    public sealed record ConversationKey(string Mode, int Repeat, string Task);

    /// <summary>Which turn: the conversation it's in, and its number.</summary>
    public sealed record TurnKey(string Mode, int Repeat, string Task, int Turn)
    {
        /// <summary>The conversation this turn belongs to.</summary>
        public ConversationKey Conversation => new(Mode, Repeat, Task);
    }

    /// <summary>Reads one line of the script's output. Anything we can't read comes back as a log line.</summary>
    /// <remarks>
    /// A switch expression on the event's name, one arm per event, read like a table; anything else is a log line.
    /// </remarks>
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
                "protocol" => new ProtocolStarted(Int(e, "version"), at, Text(e, "eval"),
                    OptionalInt(e, "eval_version"), Text(e, "commit"),
                    Graders(e), Models(e), Split(e), Text(e, "rescore_of")),
                "mode" => new ModeStarted(Required(e, "mode"), Int(e, "repeat"), Strings(e, "toolsets"),
                    Text(e, "role") ?? Text(e, "personality"), // CL14: a harness before CL14 said personality
                    Text(e, "llm"), OptionalInt(e, "conversations"), at),
                "question" => new QuestionAsked(Turn(e), Required(e, "session"), Required(e, "text"), at),
                "sse" => new SseEvent(Turn(e), Required(e, "type"), Json(e, "data"), at),
                // The agent's own calls are puls_calls on the protocol's version 1 wire, which CL22 versions.
                "turn" => new TurnEnded(Turn(e), Required(e, "session"), Required(e, "question"), Text(e, "answer"),
                    Text(e, "role"),
                    Text(e, "continued"), OptionalInt(e, "puls_calls") ?? 0, OptionalInt(e, "specialist_calls") ?? 0,
                    Strings(e, "hosts"), OptionalNumber(e, "seconds") ?? 0, Text(e, "error"), at, Failure(e)),
                "conversation" => new ConversationEnded(
                    new ConversationKey(Required(e, "mode"), Int(e, "repeat"), Required(e, "task")),
                    Text(e, "role"), Json(e, "scores"), OptionalInt(e, "errors") ?? 0,
                    OptionalNumber(e, "seconds") ?? 0, at,
                    Grades(e), Outcome(e, "outcome")),
                "mode_done" => new ModeEnded(Required(e, "mode"), Int(e, "repeat"), at, Rates(e)),
                "summary" => SummaryLineReader.Of(e, at) is { } summary ? summary : new LogLine(line),
                _ => new LogLine(line),
            };
        }
        // A broken or half-written event is kept as text, so the run carries on.
        catch (Exception ex) when (ex is JsonException or MissingField or InvalidOperationException or FormatException)
        {
            return new LogLine(line);
        }
    }

    /// <summary>
    /// Which scenarios the run asked for. Left out means all of them; a name we don't know makes the line unreadable.
    /// </summary>
    private static EvalRunSplit? Split(JsonElement e) =>
        Text(e, "split") is null ? null : Choice<EvalRunSplit>(e, "split");

    /// <summary>One slice of a mode's roll-up, written after the mode's last <c>mode_done</c>.</summary>
    public sealed record SummaryReported(EvalSlice Slice, DateTimeOffset? At = null) : Event;

    /// <summary>The eval's graders from the first line, if it names any.</summary>
    private static IReadOnlyList<GraderInfo>? Graders(JsonElement e) =>
        e.TryGetProperty("graders", out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(g => new GraderInfo(Required(g, "name"), Int(g, "version"),
                Choice<EvalGraderKind>(g, "kind"), Text(g, "question"))).ToArray()
            : null;

    /// <summary>The model each level ran on, from the first line, if it names any.</summary>
    private static IReadOnlyList<ModelInfo>? Models(JsonElement e) =>
        e.TryGetProperty("models", out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(m => new ModelInfo(Required(m, "mode"),
                Choice<EvalModelLevel>(m, "level"), Text(m, "role"), Text(m, "model"))).ToArray()
            : null;

    /// <summary>A conversation's grades, if the line carries any.</summary>
    private static IReadOnlyList<GradeInfo>? Grades(JsonElement e) =>
        e.TryGetProperty("grades", out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Select(g => new GradeInfo(Required(g, "grader"), Int(g, "version"),
                OptionalNumber(g, "score") ?? throw new MissingField(), Choice<EvalOutcome>(g, "outcome"),
                Text(g, "reason") ?? "",
                Text(g, "judge_model"), Text(g, "judge_prompt_hash"))).ToArray()
            : null;

    /// <summary>An outcome, if there is one. A word that isn't an outcome makes the line unreadable.</summary>
    private static EvalOutcome? Outcome(JsonElement e, string name) =>
        Text(e, name) is null ? null : Choice<EvalOutcome>(e, name);

    /// <summary>
    /// Reads a word that must be one of an enum's names, once, as the line comes in. Anything else makes the line
    /// unreadable.
    /// </summary>
    private static T Choice<T>(JsonElement e, string name) where T : struct, Enum =>
        SettingChoice.TryParse(Text(e, name), out T? choice) && choice is { } value ? value : throw new MissingField();

    /// <summary>
    /// What on our side broke and ended the turn, if anything. A name we don't know makes the line unreadable.
    /// </summary>
    private static EvalTurnFailure? Failure(JsonElement e) =>
        Text(e, "failure") is null ? null : Choice<EvalTurnFailure>(e, "failure");

    /// <summary>Which turn a line is about.</summary>
    private static TurnKey Turn(JsonElement e) =>
        new(Required(e, "mode"), Int(e, "repeat"), Required(e, "task"), Int(e, "turn"));

    /// <summary>Thrown when a field the event can't do without is missing or the wrong type.</summary>
    private sealed class MissingField : Exception;

    /// <summary>A text field, if it's there and is text.</summary>
    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    /// <summary>A text field the event can't do without.</summary>
    private static string Required(JsonElement e, string name) => Text(e, name) ?? throw new MissingField();

    /// <summary>
    /// A mode's rates, when the line names its k (an older line doesn't). A rate that's left out stays null, never 0.
    /// </summary>
    private static RatesInfo? Rates(JsonElement e) =>
        OptionalInt(e, "k") is { } k
            ? new RatesInfo(OptionalNumber(e, "pass_at_k"), OptionalNumber(e, "pass_hat_k"), k,
                OptionalInt(e, "infrastructure") ?? 0, OptionalInt(e, "retried") ?? 0)
            : null;

    /// <summary>A whole number the event can't do without.</summary>
    private static int Int(JsonElement e, string name) => OptionalInt(e, name) ?? throw new MissingField();

    /// <summary>A whole number, if it's there and fits.</summary>
    private static int? OptionalInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int n)
            ? n
            : null;

    /// <summary>A number, if it's there.</summary>
    private static double? OptionalNumber(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number ? value.GetDouble() : null;

    /// <summary>A list of texts, skipping anything that isn't text; empty when it's missing.</summary>
    private static IReadOnlyList<string> Strings(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? value.EnumerateArray().Where(v => v.ValueKind == JsonValueKind.String).Select(v => v.GetString()!)
                .ToArray()
            : [];

    /// <summary>A copy of the field that outlives the line's document; an empty object when it's missing.</summary>
    private static JsonElement Json(JsonElement e, string name) =>
        e.TryGetProperty(name, out var value) ? value.Clone() : EmptyObject;

    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>When the line was written: the script's <c>t</c>, in Unix seconds to the millisecond.</summary>
    private static DateTimeOffset? Time(JsonElement e) =>
        OptionalNumber(e, "t") is { } seconds
            ? DateTimeOffset.FromUnixTimeMilliseconds((long)Math.Round(seconds * 1000))
            : null;
}
