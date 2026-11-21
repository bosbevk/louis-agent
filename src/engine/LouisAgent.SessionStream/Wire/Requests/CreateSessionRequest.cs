using System.Text.Json.Serialization;

namespace LouisAgent.SessionStream.Wire.Requests;

/// <summary>
/// What you send to open a session: its toolsets (F5), its role (F21), its LLM profile (F30), who answers (F109.3),
/// where the user is (F22), and tags for the usage ledger, kept short and printable. Depth is 1 when another agent
/// opens it as a sub-session, 2 for a minion.
/// </summary>
/// <remarks>
/// Data Transfer Object (Fowler, PoEAA), shared since F127.3.1: the API reads it, and core's sub-sessions and the
/// browser write it, so a renamed field breaks every one of them at compile time instead of on the wire. It holds only
/// the data; how the API reads a field (trimmed, parsed, checked) stays in the API.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0117-one-address-for-every-agent-call.md">ADR 0117: one address for every agent call</seealso>
public sealed record CreateSessionRequest(
    string? Task,
    string? Run,
    string? Service,
    List<string>? Toolsets = null,
    int Depth = 0,
    string? Parent = null,
    string? Location = null,
    [property: JsonPropertyName("time_zone")]
    string? TimeZone = null,
    string? Llm = null,
    string? Tool = null,
    string? Role = null,
    string? Stream = null,
    ReplayRequest? Replay = null,
    string? Identity = null);
