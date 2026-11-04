using System.Net.ServerSentEvents;
using System.Text.Json;
using LouisAgent.Api.Contracts;
using LouisAgent.Choices;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Reports;
using LouisAgent.SessionStream;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Sessions.Streaming.Claude;

/// <summary>
/// Writes a turn as Claude writes its stream: the same events, in the same JSON, so a client made for Claude reads
/// ours (F18 step 1f). It was the API's first stream format.
/// </summary>
/// <remarks>
/// Strategy: one of the turn's stream formats behind <see cref="ITurnStreamFormat"/>, so the turn never asks which
/// format it is writing, and another format is one more class. Each event is a record of its own
/// (<c>ClaudeEvents</c>, <c>ClaudeBlocks</c>, <see cref="ClaudeUsage"/>), never an anonymous object.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed class ClaudeStreamFormat(
    string sessionId,
    IReadOnlyList<string>? toolsets = null,
    string? sessionRole = null) : ITurnStreamFormat
{
    // Relaxed escaping keeps quotes and backticks readable instead of '-style escapes; the data is JSON, never
    // embedded in HTML.
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private int _index = -1;
    private string? _openBlock;

    /// <summary>One event of the stream: its name, and its data as JSON.</summary>
    public static SseItem<string> Event(string type, object data) => new(JsonSerializer.Serialize(data, Json), type);

    /// <summary>
    /// A sub-session's event (F5), nested: the <c>StartSubSession</c> call it belongs to, the peer, and the peer's own
    /// event (Claude's shape), so a client can fold it into that call the way it folds the turn's own events.
    /// </summary>
    public SseItem<string> SubSession(SubSessionEvent sub)
    {
        using var data = JsonDocument.Parse(sub.Data);
        return Event("sub_session",
            new SubSessionEventData(sub.ToolUseId, sub.Agent, sub.Type, data.RootElement.Clone()));
    }

    /// <summary>
    /// The turn's start, with the toolsets the session loaded (F34: a specialist's card shows what it has) and its role
    /// (FX17: a follow-up's card names the specialist while it works); <c>session_role</c>, since <c>role</c> is
    /// Claude's speaker.
    /// </summary>
    public SseItem<string> Start() =>
        Event("message_start",
            new MessageStartEvent(new MessageStartBody(sessionId, "assistant", toolsets ?? [], sessionRole)));

    /// <summary>
    /// One piece of what the agent said or did: thinking and text arrive as deltas, a tool call and its result each as
    /// a whole block. Anything else isn't shown.
    /// </summary>
    public IEnumerable<SseItem<string>> Content(AIContent content) => content switch
    {
        TextReasoningContent { Text.Length: > 0 } thinking => Delta("thinking", new ThinkingDelta(thinking.Text)),
        TextContent { Text.Length: > 0 } text => Text(text.Text),
        FunctionCallContent call => Block(new ToolUseBlock(call.CallId, call.Name,
            call.Arguments ?? new Dictionary<string, object?>())),
        FunctionResultContent result => Block(ToolResult(result)),
        _ => [],
    };

    /// <summary>Some of the answer's text, added to the text block that's open.</summary>
    public IEnumerable<SseItem<string>> Text(string text) => Delta("text", new TextDelta(text));

    /// <summary>
    /// The turn's end: the open block closes, then the stop reason, with the usage when there is one.
    /// </summary>
    public IEnumerable<SseItem<string>> Stop(string stopReason, TurnUsage? usage)
    {
        foreach (var item in CloseBlock()) yield return item;
        yield return Event("message_delta", new MessageDeltaEvent(new StopDelta(stopReason), RenderUsage(usage)));
        yield return Event("message_stop", new MessageStopEvent());
    }

    /// <summary>
    /// A failed turn: the open block closes, then the error. The usage goes with it when there is one: an extra field
    /// (Claude's error event has none, and its clients ignore what they don't know).
    /// </summary>
    public IEnumerable<SseItem<string>> Fail(StreamErrorKind kind, string error, TurnUsage? usage)
    {
        foreach (var item in CloseBlock()) yield return item;
        // The error names its kind as Claude's does, in error.type (a replay miss its own, FX26).
        var detail = new ApiErrorDetail(kind.Wire(), error);
        object? rendered = RenderUsage(usage);
        yield return Event("error", rendered is null
            ? new ApiError("error", detail)
            : new StreamErrorEvent(detail, rendered));
    }

    /// <summary>
    /// What a turn (or a session) used, as the usage object on Claude's <c>message_delta</c>: the same four token
    /// fields, summed over every model request of the turn, plus our <c>requests</c>, <c>cost</c> and
    /// <c>currency</c>. The four kinds don't overlap (F1), as in Claude's own usage: <c>input_tokens</c> is the
    /// uncached input. Cost is null when no request was priced ("price unknown", never 0); <c>unpriced</c> counts the
    /// requests without a price.
    /// </summary>
    public object? RenderUsage(TurnUsage? usage)
    {
        if (usage is null) return null;
        UsageTotal total = usage.Total;
        string? currency = usage.Currency;
        // F14: the cost in the display currency too, beside what was charged; the same as the charged cost when not
        // converting, so a client can always show display_* and never needs the rates itself.
        DisplayCurrency? converted = usage.Display is { Converts: true } display &&
                                     string.Equals(currency, display.Base, StringComparison.OrdinalIgnoreCase)
            ? display
            : null;
        return new ClaudeUsage(total.Input, total.CacheWrite, total.CacheRead, total.Output, total.Requests,
            total.Cost, currency, total.Unpriced, DisplayCost(total.Cost, converted),
            converted?.Code ?? currency, DisplaySymbol(converted, currency), converted?.RateAsOf);
    }

    /// <summary>
    /// What a tool gave back. It counts as a failure when it threw, or when its text starts with "Error".
    /// </summary>
    private static ToolResultBlock ToolResult(FunctionResultContent result)
    {
        string? text = result.Result?.ToString();
        bool failed = result.Exception is not null ||
                      (text ?? "").StartsWith("Error", StringComparison.OrdinalIgnoreCase);
        return new ToolResultBlock(result.CallId, text ?? result.Exception?.Message ?? string.Empty, failed);
    }

    /// <summary>The cost in the display currency when we convert, else the cost as charged.</summary>
    private static decimal? DisplayCost(decimal? cost, DisplayCurrency? converted) =>
        converted is null ? cost : Math.Round(cost!.Value * converted.Rate, 6, MidpointRounding.AwayFromZero);

    /// <summary>
    /// The symbol to show the cost with: the display currency's when we convert, else the charged currency's, and
    /// none when we don't know the currency.
    /// </summary>
    /// <remarks>Guard clauses (Fowler), one per case, instead of a ternary inside a ternary.</remarks>
    private static string? DisplaySymbol(DisplayCurrency? converted, string? currency)
    {
        if (converted is not null) return converted.Symbol;
        if (currency is null) return null;
        return DisplayCurrency.SymbolFor(currency);
    }

    /// <summary>Closes the block that's open, if there is one.</summary>
    private IEnumerable<SseItem<string>> CloseBlock()
    {
        if (_openBlock is null) yield break;
        _openBlock = null;
        yield return Event("content_block_stop", new ContentBlockStopEvent(_index));
    }

    /// <summary>
    /// Streamed thinking or text: deltas of the same kind in a row share one content block, and a new kind opens its
    /// own.
    /// </summary>
    private IEnumerable<SseItem<string>> Delta(string blockType, object delta)
    {
        if (_openBlock != blockType)
        {
            foreach (var item in CloseBlock()) yield return item;
            _openBlock = blockType;
            object block = blockType == "thinking" ? new ThinkingBlock() : new TextBlock();
            yield return Event("content_block_start", new ContentBlockStartEvent(++_index, block));
        }

        yield return Event("content_block_delta", new ContentBlockDeltaEvent(_index, delta));
    }

    /// <summary>A tool call or its result: they arrive whole, so each is a complete block, opened and closed.</summary>
    private IEnumerable<SseItem<string>> Block(object block)
    {
        foreach (var item in CloseBlock()) yield return item;
        yield return Event("content_block_start", new ContentBlockStartEvent(++_index, block));
        yield return Event("content_block_stop", new ContentBlockStopEvent(_index));
    }
}
