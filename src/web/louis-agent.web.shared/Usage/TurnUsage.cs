namespace LouisAgent.Web.Shared.Usage;

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
