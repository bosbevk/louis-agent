using System.Text;
using System.Text.Json;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Trials;

/// <summary>
/// The one reader of the engine's events for the general checks: whether a turn streamed an error, what it cost and
/// in which currency, which role its first sub-session went to, which tools were called, and the error's message.
/// </summary>
/// <remarks>
/// It reads the stream as the API sends it, and never throws on an odd event: a field it can't read is read as
/// absent, so the grade is Unknown or there is no route, not a crash. The cost is the usage the stream reports on
/// message_delta (or on error), the ledger's total for the turn; no price is worked out here.
/// </remarks>
public static class EngineEvents
{
    /// <summary>The engine's own tool that hands a question to a role (core's sub-sessions).</summary>
    private const string StartSubSession = "StartSubSession";

    /// <summary>The engine's own tool that goes back to a sub-session by its handle.</summary>
    private const string ContinueSubSession = "ContinueSubSession";

    /// <summary>
    /// True when the turn streamed an error event: the agent's error. A specialist's error is not the turn's.
    /// </summary>
    public static bool StreamedError(TurnTranscript turn) => turn.Events.Any(e => e is ErrorReported);

    /// <summary>
    /// The infrastructure's failure of the turn (a timeout, a dropped connection), reported apart from the agent's
    /// errors.
    /// </summary>
    public static TurnFailure? InfrastructureFailure(TurnTranscript turn) => turn.Failure;

    /// <summary>True when the turn reported usage at all.</summary>
    public static bool HasUsage(TurnTranscript turn) => Usage(turn) is not null;

    /// <summary>The currency the turn's usage is in, or null when it doesn't say.</summary>
    public static string? CurrencyOf(TurnTranscript turn) => Usage(turn) is { } usage ? Text(usage, "currency") : null;

    /// <summary>
    /// The turn's cost in the ledger's currency; null when unknown (no usage, no cost, or a request with no price).
    /// </summary>
    public static decimal? CostOf(TurnTranscript turn)
    {
        if (Usage(turn) is not { } usage) return null;
        // An absent count is no unpriced request; any other count that isn't zero, or isn't a number, leaves the
        // total incomplete.
        if (Field(usage, "unpriced", out var unpriced)
            && !(unpriced.ValueKind == JsonValueKind.Number && unpriced.TryGetInt32(out var count) && count == 0))
            return null;
        return Field(usage, "cost", out var cost) && cost.ValueKind == JsonValueKind.Number &&
               cost.TryGetDecimal(out var value)
            ? value
            : null;
    }

    /// <summary>
    /// The role of the turn's first StartSubSession call that names a role, or null when the agent answered itself
    /// or no such call names one.
    /// </summary>
    public static string? RoutedRole(TurnTranscript turn)
    {
        foreach (var e in turn.Events)
        {
            if (e is not ContentBlockStarted || !Field(e.Data, "content_block", out var block)) continue;
            if (Text(block, "type") == "tool_use" && Text(block, "name") == StartSubSession
                                                  && Field(block, "input", out var input) &&
                                                  Text(input, "role") is { } role && !string.IsNullOrWhiteSpace(role))
                return role;
        }

        return null;
    }

    /// <summary>The names of the tools the turn's agent called itself, in order.</summary>
    public static IReadOnlyList<string> ToolsCalled(TurnTranscript turn) => CalledIn(turn.Events);

    /// <summary>
    /// The names of the tools the turn's specialists called, in order: read from the sub_session events that carry
    /// their streams.
    /// </summary>
    public static IReadOnlyList<string> SpecialistToolsCalled(TurnTranscript turn) => CalledIn(SpecialistEvents(turn));

    /// <summary>
    /// The handle of the sub-session the turn's agent went back to (its first ContinueSubSession call that names
    /// one), or null when it went back to none.
    /// </summary>
    public static string? ContinuedHandle(TurnTranscript turn) =>
        ToolBlocks(turn.Events).Where(block => Text(block, "name") == ContinueSubSession)
            .Select(block => Field(block, "input", out var input) ? Text(input, "handle") : null)
            .FirstOrDefault(handle => !string.IsNullOrEmpty(handle));

    /// <summary>
    /// The URLs the turn's tool calls were given, the agent's first and then its specialists', in order.
    /// </summary>
    public static IReadOnlyList<string> UrlsGiven(TurnTranscript turn) =>
    [
        .. ToolBlocks(turn.Events.Concat(SpecialistEvents(turn)))
            .Select(block => Field(block, "input", out var input) ? Text(input, "url") : null)
            .OfType<string>()
    ];

    /// <summary>The message of the turn's error event, or null when it has none.</summary>
    public static string? ErrorMessageOf(TurnTranscript turn)
    {
        var error = turn.Events.OfType<ErrorReported>().FirstOrDefault();
        return error is not null && Field(error.Data, "error", out var detail) ? Text(detail, "message") : null;
    }

    /// <summary>
    /// The turn's error as text: its message, else the error event's JSON when it has none; null when the turn
    /// streamed no error.
    /// </summary>
    public static string? ErrorTextOf(TurnTranscript turn)
    {
        var error = turn.Events.OfType<ErrorReported>().FirstOrDefault();
        if (error is null) return null;
        return ErrorMessageOf(turn) is { Length: > 0 } message ? message : error.Data.GetRawText();
    }

    /// <summary>
    /// The turn's own answer: its text deltas joined in order and trimmed, as <c>TurnView.Answer</c> reads them. A
    /// sub-session's text is not the answer, and an odd delta (no type, a text that isn't a string) is skipped, never
    /// thrown on.
    /// </summary>
    public static string AnswerOf(TurnTranscript turn)
    {
        var text = new StringBuilder();
        foreach (var e in turn.Events)
            if (e is ContentBlockDelta && Field(e.Data, "delta", out var delta) && Text(delta, "type") == "text_delta"
                && Text(delta, "text") is { } piece)
                text.Append(piece);
        return text.ToString().Trim();
    }

    /// <summary>
    /// The names of the tool calls among these events, skipping a block that isn't a call or has no name.
    /// </summary>
    private static IReadOnlyList<string> CalledIn(IEnumerable<SessionEvent> events) =>
        [.. ToolBlocks(events).Select(block => Text(block, "name")).OfType<string>()];

    /// <summary>The tool-call blocks among these events, in order: each one's name and input.</summary>
    private static IEnumerable<JsonElement> ToolBlocks(IEnumerable<SessionEvent> events) =>
        events.OfType<ContentBlockStarted>()
            .Select(started => Field(started.Data, "content_block", out var block) ? block : default)
            .Where(block => Text(block, "type") == "tool_use");

    /// <summary>The specialists' events, unwrapped from the sub_session events that carry their streams.</summary>
    private static IEnumerable<SessionEvent> SpecialistEvents(TurnTranscript turn) =>
        turn.Events.OfType<SubSessionStreamed>().Select(sub => sub.Inner);

    /// <summary>
    /// The usage object on the turn's last message_delta, or on its error when it failed: the stream's end. Null when
    /// there is none.
    /// </summary>
    private static JsonElement? Usage(TurnTranscript turn)
    {
        var end = turn.Events.LastOrDefault(e => e is MessageDelta or ErrorReported);
        return end is not null && Field(end.Data, "usage", out var usage) && usage.ValueKind == JsonValueKind.Object
            ? usage
            : null;
    }

    /// <summary>A field of a JSON object; false when the element is not an object, or has no such field.</summary>
    private static bool Field(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object) return element.TryGetProperty(name, out value);
        value = default;
        return false;
    }

    /// <summary>A string field of a JSON object; null when it is absent or isn't a string.</summary>
    private static string? Text(JsonElement element, string name) =>
        Field(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
