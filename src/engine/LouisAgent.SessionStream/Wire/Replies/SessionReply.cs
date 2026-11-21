namespace LouisAgent.SessionStream.Wire.Replies;

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
    string? Identity,
    string? Llm,
    string? Tool,
    int Tools,
    string? Task,
    string? Run,
    string? Service,
    IReadOnlyList<SubSessionRow> SubSessions,
    object? Usage);
