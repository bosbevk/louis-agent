namespace LouisAgent.Core.Usage;

using System.Text.Json.Serialization;

/// <summary>
/// One model request in the usage ledger (Usage spec §4.1). Holds counts, ids, names and timings only: never prompt text
/// or tool output (F1-S4).
/// </summary>
/// <param name="At">When the request finished.</param>
/// <param name="Round">Model request number within the turn, 1-based (F1-S1).</param>
/// <param name="Purpose">One of <see cref="UsagePurpose"/>.</param>
/// <param name="Host">cli | acp | api | orchestrator, from the host's <see cref="UsageScope"/>; null outside one (F1-S2).</param>
/// <param name="Session">Session id (F1-S2).</param>
/// <param name="Turn">User message number in the session (F1-S2).</param>
/// <param name="Task">For example "fix:e8b0b572223d", set by the caller through the API (F1-S3).</param>
/// <param name="Run">Orchestrator run id, on both the triage and the fix records (F1-S3).</param>
/// <param name="Service">Service the run is fixing (F1-S3).</param>
/// <param name="Model">Model id from the response, falling back to the client's configured model.</param>
/// <param name="Tokens">The four non-overlapping token kinds plus reasoning.</param>
/// <param name="DurationMs">Wall time of the request, from the call to the last streamed update.</param>
/// <param name="Stop">Finish reason (stop, tool_calls, length, …), or "cancelled".</param>
/// <param name="Cost">What the request cost from the price table (F2); null means "price unknown", never free.</param>
/// <param name="Parent">For a sub-session's request, the parent turn, <c>{session}#{turn}</c> (F5); null otherwise.</param>
/// <param name="Requests">
/// How many model requests a <see cref="UsagePurpose.SubSession"/> roll-up stands for (F5); null, meaning one, on every
/// other record, and then left out of its ledger line, so the format of a model request's row is unchanged.
/// </param>
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
    string? Stop,
    UsageCost? Cost = null,
    string? Parent = null,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Requests = null);
