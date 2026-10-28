namespace LouisAgent.Api.Sessions.Streaming.Claude;

/// <summary>
/// What a turn (or a session) used, as Claude's usage object plus ours: the four token counts, how many requests, the
/// cost as charged and in the display currency. A cost nobody could price is null, never 0.
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA): the usage is a named record, so the turn's stream and the session view write
/// the same shape from one place.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
internal sealed record ClaudeUsage(
    long InputTokens,
    long CacheCreationInputTokens,
    long CacheReadInputTokens,
    long OutputTokens,
    int Requests,
    decimal? Cost,
    string? Currency,
    int Unpriced,
    decimal? DisplayCost,
    string? DisplayCurrency,
    string? DisplaySymbol,
    string? RateAsOf);
