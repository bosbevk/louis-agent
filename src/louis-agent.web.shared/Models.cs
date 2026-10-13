namespace LouisAgent.Web.Shared;

/// <summary>A chat as the browser keeps it; the server only holds the session (and loses it on restart).</summary>
public sealed class Chat
{
    public required string SessionId { get; init; }
    public string Title { get; set; } = "New chat";
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.Now;
    public List<Turn> Turns { get; init; } = [];

    /// <summary>The API no longer knows this session (it was restarted), so the chat can be read but not continued.</summary>
    public bool Ended { get; set; }

    /// <summary>
    /// The chat's usage: the sum of its turns' (F3). Added up here, not asked from the API, because the browser keeps the
    /// chat after the API's session is gone.
    /// </summary>
    public TurnUsage? Usage() =>
        Turns.Select(t => t.Usage).OfType<TurnUsage>().Aggregate((TurnUsage?)null, (total, usage) => total is null ? usage : total + usage);
}

/// <summary>One user message and the agent's streamed answer to it.</summary>
public sealed class Turn
{
    public string UserText { get; init; } = "";
    public List<string> AttachmentNames { get; init; } = [];
    public List<Block> Blocks { get; init; } = [];
    public string? StopReason { get; set; }
    public string? Error { get; set; }

    /// <summary>What the turn's model requests used, from <c>message_delta.usage</c>; null for commands and older chats.</summary>
    public TurnUsage? Usage { get; set; }
}

/// <summary>
/// <c>message_delta.usage</c> from louis-agent.api (F3): Claude's four token fields summed over a turn's model requests,
/// plus how many requests and what they cost. <see cref="Cost"/> is null when no request was priced ("price unknown").
/// </summary>
public sealed record TurnUsage(
    long InputTokens, long CacheCreationInputTokens, long CacheReadInputTokens, long OutputTokens,
    int Requests, decimal? Cost, string? Currency, int Unpriced)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public long TotalTokens => InputTokens + CacheCreationInputTokens + CacheReadInputTokens + OutputTokens;

    /// <summary>Adds two turns up; a cost is only unknown when neither side has one, as on the server.</summary>
    public static TurnUsage operator +(TurnUsage a, TurnUsage b) => new(
        a.InputTokens + b.InputTokens,
        a.CacheCreationInputTokens + b.CacheCreationInputTokens,
        a.CacheReadInputTokens + b.CacheReadInputTokens,
        a.OutputTokens + b.OutputTokens,
        a.Requests + b.Requests,
        a.Cost is null && b.Cost is null ? null : (a.Cost ?? 0) + (b.Cost ?? 0),
        a.Currency ?? b.Currency,
        a.Unpriced + b.Unpriced);
}

public enum BlockKind { Thinking, Text, Tool }

/// <summary>A piece of the answer: thinking, text, or a tool call with its result.</summary>
public sealed class Block
{
    public BlockKind Kind { get; init; }
    public string Text { get; set; } = "";

    public double? ThinkingSeconds { get; set; }
    public DateTimeOffset StartedAt { get; init; } = DateTimeOffset.Now;

    public string? ToolUseId { get; init; }
    public string? ToolName { get; init; }
    public string? ToolInput { get; init; }
    public string? ToolResult { get; set; }
    public bool ToolFailed { get; set; }

    /// <summary>
    /// For a <c>StartSubSession</c> call (F5): the specialist's own turn (its thinking, tool calls and answer), folded from
    /// the sub-session events the API streams while the call runs. Null for other tools, and for chats from before.
    /// </summary>
    public Turn? SubTurn { get; set; }
}

public sealed record Attachment(string Name, string Content);

/// <summary>The API's <c>GET /rates</c> (F14), in louis-agent's rates.json format: one <see cref="Base"/> in each currency.</summary>
public sealed record RatesTable(
    [property: System.Text.Json.Serialization.JsonPropertyName("as_of")] string AsOf,
    string Base,
    Dictionary<string, decimal> Rates);

/// <summary>The API's <c>GET /rates/display</c> (F14): the server's display currency (the PC's region) and a symbol per currency.</summary>
public sealed record RatesDisplay(
    [property: System.Text.Json.Serialization.JsonPropertyName("display_currency")] string DisplayCurrency,
    [property: System.Text.Json.Serialization.JsonPropertyName("display_symbol")] string DisplaySymbol,
    string Base,
    Dictionary<string, string> Symbols);

/// <summary>
/// The currency this browser shows costs in (F14): costs are stored as charged (in <see cref="Base"/>) and converted at
/// <see cref="Rate"/> only when shown, so changing it re-labels every saved chat.
/// </summary>
public sealed record CurrencyChoice(string Code, string Symbol, decimal Rate, string? AsOf, string Base)
{
    public bool Converts => Code != Base;
}
