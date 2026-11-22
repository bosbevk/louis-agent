using LouisAgent.Api.Sessions.Streaming;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Api.Sessions.Opening;

/// <summary>
/// What a session is asked to open with: its toolsets (none named: the default; empty: none), the tags its usage
/// carries, how deep it sits, its role or identity, the user's place, the model it runs on, a lookup agent's one tool,
/// the stream's format, a replay and who opened it. A caller names only what it sets; the rest is today's default.
/// </summary>
/// <remarks>
/// Fowler's <i>Introduce Parameter Object</i> (CL70, Louis's pick A of three: "why is this so ugly"): eleven values
/// travelled as eleven parameters, most of them optional, so every call had to know their order. One record carries
/// them, and the session's opening reads it.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0040-records-for-values-classes-for-things.md">ADR 0040: records for values, classes for things</seealso>
internal sealed record SessionRequest(
    IEnumerable<string>? Toolsets = null,
    UsageTags? Tags = null,
    int Depth = 0,
    string? Role = null,
    UserPlace? Place = null,
    string? Llm = null,
    string? Tool = null,
    StreamFormat Format = StreamFormat.Claude,
    SessionReplay? Replay = null,
    SessionOpener? Opener = null,
    string? Identity = null);
