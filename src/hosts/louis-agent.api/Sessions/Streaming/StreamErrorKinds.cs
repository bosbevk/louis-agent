using LouisAgent.Core.Llm.Replay;
using LouisAgent.SessionStream;

namespace LouisAgent.Api.Sessions.Streaming;

/// <summary>
/// The kind a failed turn's error event names for what went wrong: a replay with no recorded answer is a replay miss,
/// anything else is the API's own failure.
/// </summary>
/// <remarks>
/// The one place the API turns an exception into the stream's kind (FX26), so the stream's format only writes what
/// it is handed, and a new kind is one more arm here.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal static class StreamErrorKinds
{
    /// <summary>The kind of failure this exception was, as the stream says it.</summary>
    public static StreamErrorKind Of(Exception error) => error is ModelReplayMissException
        ? StreamErrorKind.ReplayMiss
        : StreamErrorKind.Api;
}
