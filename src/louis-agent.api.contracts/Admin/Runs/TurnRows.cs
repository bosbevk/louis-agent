namespace LouisAgent.Api.Contracts.Admin.Runs;

/// <summary>
/// One turn of a stored conversation: what its tools did, which turn it is, the question and the answer, the calls it
/// made, the sites it read, its time and its error.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record TurnRow(
    TurnToolsRow Tools,
    string Mode,
    int Repeat,
    string Task,
    int Turn,
    string Session,
    string Question,
    string? Answer,
    string? Role,
    string? Continued,
    int PulsCalls,
    int SpecialistCalls,
    IReadOnlyList<string> Hosts,
    double Seconds,
    string? Error);

/// <summary>What a turn's tools did: how many calls, how many failed, and why, per tool.</summary>
public sealed record TurnToolsRow(int Calls, int Failed, IReadOnlyList<ToolReasonRow> Reasons);

/// <summary>One tool's reason for failing, and how often.</summary>
public sealed record ToolReasonRow(string Tool, string Reason, int Count);

/// <summary>
/// One model request from the ledger: which turn asked, why, the model, its tokens, cost and time. Tokens and cost
/// the ledger didn't know are null, never 0.
/// </summary>
public sealed record RequestRow(
    TurnKeyRow? Turn,
    DateTimeOffset At,
    string Purpose,
    string? Session,
    string? Parent,
    string? Model,
    long? InputTokens,
    long? OutputTokens,
    long? CacheReadTokens,
    long? CacheWriteTokens,
    decimal? Cost,
    long DurationMs);

/// <summary>Which turn: its mode, repeat, task and number.</summary>
public sealed record TurnKeyRow(string Mode, int Repeat, string Task, int Turn);
