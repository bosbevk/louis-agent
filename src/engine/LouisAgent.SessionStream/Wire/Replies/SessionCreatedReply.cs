namespace LouisAgent.SessionStream.Wire.Replies;

/// <summary>
/// What a session opened with: its id and time, its toolsets, depth, role, LLM profile and minion tool, and the usage
/// tags it puts on every record.
/// </summary>
/// <remarks>
/// Shared since F127.3.1: the API writes it, and core's sub-sessions, the browser, the eval runner and the orchestrator
/// read it typed, never a property picked out by name.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public sealed record SessionCreatedReply(
    string SessionId,
    DateTimeOffset CreatedAt,
    IReadOnlyList<string> Toolsets,
    int Depth,
    string? Role,
    string? Identity,
    string? Llm,
    string? Tool,
    string? Task,
    string? Run,
    string? Service,
    string? Parent);
