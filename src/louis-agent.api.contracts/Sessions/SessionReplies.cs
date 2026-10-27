namespace LouisAgent.Api.Contracts.Sessions;

/// <summary>
/// What a session opened with: its id and time, its toolsets, depth, role, LLM profile and minion tool, and the usage
/// tags it puts on every record.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record SessionCreatedReply(
    string SessionId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Toolsets,
    int Depth,
    string? Role,
    string? Llm,
    string? Tool,
    string? Task,
    string? Run,
    string? Service,
    string? Parent);

/// <summary>
/// A session now: whether it's busy, its tools, the specialists it keeps open by handle (never the peer's session id)
/// and its usage in the stream format's shape (null when the API doesn't record usage).
/// </summary>
public sealed record SessionReply(
    string SessionId,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivity,
    bool Busy,
    IReadOnlyList<string> Toolsets,
    int Depth,
    string? Role,
    string? Llm,
    string? Tool,
    int Tools,
    string? Task,
    string? Run,
    string? Service,
    IReadOnlyList<SubSessionRow> SubSessions,
    object? Usage);

/// <summary>A specialist a session keeps open: its handle, which agent, its toolsets and role.</summary>
public sealed record SubSessionRow(string Handle, string Agent, IReadOnlyList<string> Toolsets, string? Role);

/// <summary>A session that was deleted.</summary>
public sealed record SessionDeletedReply(string SessionId, bool Deleted);

/// <summary>A cancel: whether a turn was running to cancel.</summary>
public sealed record SessionCancelledReply(string SessionId, bool Cancelled);
