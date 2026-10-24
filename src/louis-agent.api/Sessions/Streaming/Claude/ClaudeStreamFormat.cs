using System.Net.ServerSentEvents;
using System.Text.Json;
using LouisAgent.Api.Endpoints;
using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Reports;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Sessions.Streaming.Claude;

/// <summary>Turns streamed agent updates into Claude-style SSE events with JSON data: the API's first stream format (F18 step 1f).</summary>
internal sealed class ClaudeStreamFormat(string sessionId, IReadOnlyList<string>? toolsets = null, string? sessionRole = null) : ITurnStreamFormat
{
    // Relaxed escaping keeps quotes and backticks readable instead of '-style escapes; the data is JSON, never embedded in HTML.
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private int _index = -1;
    private string? _openBlock;

    public static SseItem<string> Event(string type, object data) => new(JsonSerializer.Serialize(data, Json), type);

    /// <summary>
    /// A sub-session's event (F5), nested: the <c>StartSubSession</c> call it belongs to, the peer, and the peer's own event
    /// (Claude's shape), so a client can fold it into that call the way it folds the turn's own events.
    /// </summary>
    public SseItem<string> SubSession(SubSessionEvent sub)
    {
        using var data = JsonDocument.Parse(sub.Data);
        return Event("sub_session", new
        {
            type = "sub_session", tool_use_id = sub.ToolUseId, agent = sub.Agent, @event = sub.Type, data = data.RootElement.Clone(),
        });
    }

    /// <summary>
    /// The turn's start, with the toolsets the session loaded (F34: a specialist's card shows what it has) and its role
    /// (FX17: a follow-up's card names the specialist while it works); <c>session_role</c>, since <c>role</c> is Claude's speaker.
    /// </summary>
    public SseItem<string> Start() =>
        Event("message_start", new { type = "message_start", message = new { session_id = sessionId, role = "assistant", toolsets = toolsets ?? [], session_role = sessionRole } });

    public IEnumerable<SseItem<string>> Content(AIContent content) => content switch
    {
        TextReasoningContent { Text.Length: > 0 } thinking => Delta("thinking", new { type = "thinking_delta", thinking = thinking.Text }),
        TextContent { Text.Length: > 0 } text => Text(text.Text),
        FunctionCallContent call => Block(new { type = "tool_use", id = call.CallId, name = call.Name, input = call.Arguments ?? new Dictionary<string, object?>() }),
        FunctionResultContent result => Block(new
        {
            type = "tool_result",
            tool_use_id = result.CallId,
            content = result.Result?.ToString() ?? result.Exception?.Message ?? string.Empty,
            is_error = result.Exception is not null ||
                       (result.Result?.ToString() ?? "").StartsWith("Error", StringComparison.OrdinalIgnoreCase),
        }),
        _ => [],
    };

    public IEnumerable<SseItem<string>> Text(string text) => Delta("text", new { type = "text_delta", text });

    /// <summary>The turn's end: the open block closes, then the stop reason, with the usage when there is one.</summary>
    public IEnumerable<SseItem<string>> Stop(string stopReason, TurnUsage? usage)
    {
        foreach (var item in CloseBlock()) yield return item;
        object? rendered = RenderUsage(usage);
        yield return Event("message_delta", rendered is null
            ? (object)new { type = "message_delta", delta = new { stop_reason = stopReason } }
            : new { type = "message_delta", delta = new { stop_reason = stopReason }, usage = rendered });
        yield return Event("message_stop", new { type = "message_stop" });
    }

    /// <summary>
    /// A failed turn: the open block closes, then the error. The usage goes with it when there is one: an extra field
    /// (Claude's error event has none, and its clients ignore what they don't know).
    /// </summary>
    public IEnumerable<SseItem<string>> Fail(string error, TurnUsage? usage)
    {
        foreach (var item in CloseBlock()) yield return item;
        object? rendered = RenderUsage(usage);
        yield return Event("error", rendered is null
            ? ApiErrors.Body("api_error", error)
            : new { type = "error", error = new { type = "api_error", message = error }, usage = rendered });
    }

    /// <summary>
    /// What a turn (or a session) used, as the usage object on Claude's <c>message_delta</c>: the same four token fields,
    /// summed over every model request of the turn, plus our <c>requests</c>, <c>cost</c> and <c>currency</c>. The four
    /// kinds don't overlap (F1), as in Claude's own usage: <c>input_tokens</c> is the uncached input. Cost is null when
    /// no request was priced ("price unknown", never 0); <c>unpriced</c> counts the requests without a price.
    /// </summary>
    public object? RenderUsage(TurnUsage? usage)
    {
        if (usage is null) return null;
        UsageTotal total = usage.Total;
        string? currency = usage.Currency;
        DisplayCurrency? display = usage.Display;
        // F14: the cost in the display currency too, beside what was charged; the same as the charged cost when not
        // converting, so a client can always show display_* and never needs the rates itself.
        bool converts = display is { Converts: true } && string.Equals(currency, display.Base, StringComparison.OrdinalIgnoreCase);
        return new
        {
            input_tokens = total.Input,
            cache_creation_input_tokens = total.CacheWrite,
            cache_read_input_tokens = total.CacheRead,
            output_tokens = total.Output,
            requests = total.Requests,
            cost = total.Cost,
            currency,
            unpriced = total.Unpriced,
            display_cost = converts ? Math.Round(total.Cost!.Value * display!.Rate, 6, MidpointRounding.AwayFromZero) : total.Cost,
            display_currency = converts ? display!.Code : currency,
            display_symbol = converts ? display!.Symbol : currency is null ? null : DisplayCurrency.SymbolFor(currency),
            rate_as_of = converts ? display!.RateAsOf : null,
        };
    }

    private IEnumerable<SseItem<string>> CloseBlock()
    {
        if (_openBlock is null) yield break;
        _openBlock = null;
        yield return Event("content_block_stop", new { type = "content_block_stop", index = _index });
    }

    // Streamed thinking/text: consecutive deltas of the same kind share one content block.
    private IEnumerable<SseItem<string>> Delta(string blockType, object delta)
    {
        if (_openBlock != blockType)
        {
            foreach (var item in CloseBlock()) yield return item;
            _openBlock = blockType;
            object block = blockType == "thinking" ? new { type = "thinking", thinking = "" } : new { type = "text", text = "" };
            yield return Event("content_block_start", new { type = "content_block_start", index = ++_index, content_block = block });
        }

        yield return Event("content_block_delta", new { type = "content_block_delta", index = _index, delta });
    }

    // Tool calls and results arrive whole, so each is a complete block.
    private IEnumerable<SseItem<string>> Block(object block)
    {
        foreach (var item in CloseBlock()) yield return item;
        yield return Event("content_block_start", new { type = "content_block_start", index = ++_index, content_block = block });
        yield return Event("content_block_stop", new { type = "content_block_stop", index = _index });
    }
}
