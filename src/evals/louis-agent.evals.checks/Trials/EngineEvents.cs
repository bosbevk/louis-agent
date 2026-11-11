using System.Text;
using System.Text.Json;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Trials;

/// <summary>
/// The one reader of the engine's events for the general checks: whether a turn streamed an error, what it cost and
/// in which currency, which address it asked first and which it asked again, which tools were called and what they
/// gave back, and
/// the error's message.
/// </summary>
/// <remarks>
/// It reads the stream as the API sends it, and never throws on an odd event: a field it can't read is read as
/// absent, so the grade is Unknown or there is no route, not a crash. The cost is the usage the stream reports on
/// message_delta (or on error), the ledger's total for the turn; no price is worked out here.
/// </remarks>
public static class EngineEvents
{
    /// <summary>The engine's own tool that asks another agent by its address (F127.3.1).</summary>
    private const string AskAgent = "AskAgent";

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
    public static string? CurrencyOf(TurnTranscript turn) =>
        Usage(turn) is { } usage ? TextOf(usage, "currency") : null;

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
    /// The turn's tokens: its usage's input, cache-write, cache-read and output counts summed. Null when the turn
    /// reported no usage, or a count is missing or isn't a number, so a total is never quietly short.
    /// </summary>
    public static long? TokensOf(TurnTranscript turn)
    {
        if (Usage(turn) is not { } usage) return null;
        long total = 0;
        foreach (var name in TokenCounts)
        {
            if (!Field(usage, name, out var count) || count.ValueKind != JsonValueKind.Number ||
                !count.TryGetInt64(out var value))
                return null;
            total += value;
        }

        return total;
    }

    /// <summary>
    /// The four token counts a turn's usage reports, which together are every token the turn was billed for.
    /// </summary>
    private static readonly string[] TokenCounts =
        ["input_tokens", "cache_creation_input_tokens", "cache_read_input_tokens", "output_tokens"];

    /// <summary>
    /// The address of the turn's first ask (F127.3.1), or null when the agent answered itself or no ask names one.
    /// </summary>
    public static string? RoutedAddress(TurnTranscript turn) =>
        AddressesAsked(turn) is [var first, ..] ? first : null;

    /// <summary>
    /// The addresses the turn's own agent asked, in order, each once; a blank one is none, and a specialist's asks
    /// are its own.
    /// </summary>
    private static IReadOnlyList<string> AddressesAsked(TurnTranscript turn) =>
    [
        .. ToolBlocks(turn.Events).Where(block => TextOf(block, "name") == AskAgent)
            .Select(block => Field(block, "input", out var input) ? TextOf(input, "address") : null)
            .Where(address => !string.IsNullOrWhiteSpace(address)).Select(address => address!.Trim()).Distinct()
    ];

    /// <summary>The names of the tools the turn's agent called itself, in order.</summary>
    public static IReadOnlyList<string> ToolsCalled(TurnTranscript turn) => CalledIn(turn.Events);

    /// <summary>
    /// The names of the tools the turn's specialists called, in order: read from the sub_session events that carry
    /// their streams.
    /// </summary>
    public static IReadOnlyList<string> SpecialistToolsCalled(TurnTranscript turn) => CalledIn(SpecialistEvents(turn));

    /// <summary>
    /// The inputs of the turn's calls to this tool, the agent's own first and then its specialists', in order.
    /// </summary>
    public static IReadOnlyList<JsonElement> ToolInputsOf(TurnTranscript turn, string tool) =>
    [
        .. ToolBlocks(turn.Events).Concat(ToolBlocks(SpecialistEvents(turn)))
            .Where(block => TextOf(block, "name") == tool)
            .Select(block => Field(block, "input", out var input) ? input : default)
    ];

    /// <summary>
    /// The texts the turn's tools gave back, the agent's first and then its specialists', in order: a fetched page,
    /// a search's results.
    /// </summary>
    public static IReadOnlyList<string> ToolResultsOf(TurnTranscript turn) =>
    [
        .. Blocks(turn.Events, "tool_result").Concat(Blocks(SpecialistEvents(turn), "tool_result")).Select(ResultText)
    ];

    /// <summary>
    /// The texts one tool gave back in the turn, the agent's calls and its specialists' together in the order the
    /// calls were made, each result found by its call's id. A call that got no result adds nothing.
    /// </summary>
    public static IReadOnlyList<string> ResultsOfTool(TurnTranscript turn, string tool)
    {
        // A specialist's events sit in the stream where it made them, so unwrapping them in place keeps the order.
        var events = turn.Events.Select(e => e is SubSessionStreamed sub ? sub.Inner : e).ToList();
        var results = new Dictionary<string, string>();
        foreach (var block in Blocks(events, "tool_result"))
            if (TextOf(block, "tool_use_id") is { } id)
                results.TryAdd(id, ResultText(block));

        return
        [
            .. ToolBlocks(events).Where(block => TextOf(block, "name") == tool)
                .Select(block => TextOf(block, "id")).OfType<string>()
                .Where(results.ContainsKey).Select(id => results[id])
        ];
    }

    /// <summary>
    /// The address the turn asked again (F127.3.1): its first ask to an address an earlier turn of the conversation
    /// asked, or null when it asked none again.
    /// </summary>
    public static string? AskedAgain(TurnTranscript turn, IReadOnlyList<TurnTranscript> earlier)
    {
        var before = earlier.SelectMany(AddressesAsked).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return AddressesAsked(turn).FirstOrDefault(before.Contains);
    }

    /// <summary>
    /// The URLs the turn's tool calls were given, the agent's first and then its specialists', in order.
    /// </summary>
    public static IReadOnlyList<string> UrlsGiven(TurnTranscript turn) =>
    [
        .. ToolBlocks(turn.Events.Concat(SpecialistEvents(turn)))
            .Select(block => Field(block, "input", out var input) ? TextOf(input, "url") : null)
            .OfType<string>()
    ];

    /// <summary>The message of the turn's error event, or null when it has none.</summary>
    public static string? ErrorMessageOf(TurnTranscript turn)
    {
        var error = turn.Events.OfType<ErrorReported>().FirstOrDefault();
        return error is not null && Field(error.Data, "error", out var detail) ? TextOf(detail, "message") : null;
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
            if (e is ContentBlockDelta && Field(e.Data, "delta", out var delta) && TextOf(delta, "type") == "text_delta"
                && TextOf(delta, "text") is { } piece)
                text.Append(piece);
        return text.ToString().Trim();
    }

    /// <summary>
    /// The names of the tool calls among these events, skipping a block that isn't a call or has no name.
    /// </summary>
    private static IReadOnlyList<string> CalledIn(IEnumerable<SessionEvent> events) =>
        [.. ToolBlocks(events).Select(block => TextOf(block, "name")).OfType<string>()];

    /// <summary>The tool-call blocks among these events, in order: each one's name and input.</summary>
    private static IEnumerable<JsonElement> ToolBlocks(IEnumerable<SessionEvent> events) => Blocks(events, "tool_use");

    /// <summary>The content blocks of one type among these events, in order.</summary>
    private static IEnumerable<JsonElement> Blocks(IEnumerable<SessionEvent> events, string type) =>
        events.OfType<ContentBlockStarted>()
            .Select(started => Field(started.Data, "content_block", out var block) ? block : default)
            .Where(block => TextOf(block, "type") == type);

    /// <summary>
    /// A tool result's text: its content when it's a string, else its JSON (a list of blocks, say), as Puls's
    /// <c>TurnView</c> read it; empty when it has none.
    /// </summary>
    private static string ResultText(JsonElement block)
    {
        if (!Field(block, "content", out var content)) return "";
        return content.ValueKind == JsonValueKind.String ? content.GetString() ?? "" : content.GetRawText();
    }

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

    /// <summary>
    /// A string field of a JSON object, such as a tool input's <c>url</c>; null when it is absent, isn't a string, or
    /// the element isn't an object. Public so a pack's check reads a tool's input the way the harness does.
    /// </summary>
    public static string? TextOf(JsonElement element, string name) =>
        Field(element, name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}
