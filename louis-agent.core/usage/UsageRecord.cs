namespace louis_agent.core.usage;

/// <summary>
/// One model request in the usage ledger (Usage spec §4.1). Holds counts, ids, names and timings only: never prompt text
/// or tool output (F1-S4).
/// </summary>
/// <param name="At">When the request finished.</param>
/// <param name="Round">Model request number within the turn, 1-based (F1-S1).</param>
/// <param name="Purpose">One of <see cref="UsagePurpose"/>.</param>
/// <param name="Host">cli | acp | api | orchestrator. Set from the scope in F1-S2; null until then.</param>
/// <param name="Session">Session id (F1-S2).</param>
/// <param name="Turn">User message number in the session (F1-S2).</param>
/// <param name="Task">For example "fix:e8b0b572223d", set by the caller through the API (F1-S3).</param>
/// <param name="Run">Orchestrator run id (F1-S3).</param>
/// <param name="Service">Service the run is fixing (F1-S3).</param>
/// <param name="Model">Model id from the response, falling back to the client's configured model.</param>
/// <param name="Tokens">The four non-overlapping token kinds plus reasoning.</param>
/// <param name="DurationMs">Wall time of the request, from the call to the last streamed update.</param>
/// <param name="Stop">Finish reason (stop, tool_calls, length, …), or "cancelled".</param>
public sealed record UsageRecord(
    DateTimeOffset At,
    int Round,
    string Purpose,
    string? Host,
    string? Session,
    int? Turn,
    string? Task,
    string? Run,
    string? Service,
    string? Model,
    UsageTokens Tokens,
    long DurationMs,
    string? Stop);
// TODO F2: add Cost.

/// <summary>
/// Token counts for one request. The four kinds don't overlap, so they can be added up and priced separately. A count
/// the provider didn't report is null, never 0 (F1-S1).
/// </summary>
/// <remarks>Built from a provider's usage by an <see cref="IUsageMapper"/>.</remarks>
public sealed record UsageTokens(long? Input, long? CacheWrite, long? CacheRead, long? Output, long? Reasoning);

/// <summary>Why a request was made. Strings, so the ledger stays readable as JSON.</summary>
public static class UsagePurpose
{
    public const string Turn = "turn";
    public const string Continue = "continue";
    public const string Summary = "summary";
    public const string Compaction = "compaction";
    public const string Triage = "triage";
}