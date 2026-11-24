using LouisAgent.Api.Messages;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Llm.Replay;
using LouisAgent.SessionStream;

namespace LouisAgent.Api.Sessions.Streaming;

/// <summary>
/// What a failed turn's error event says went wrong: its kind, and the words the client shows. A replay with no
/// recorded answer is a replay miss and keeps its own message; anything else is the API's own failure, and the
/// person in the chat reads a plain sentence while the exception goes to the log.
/// </summary>
/// <remarks>
/// The one place the API turns an exception into what the stream says (FX26, FX239), so the stream's format only
/// writes what it is handed, and a new kind is one more arm here. The exception's own text never reaches a visitor:
/// it can name the provider, its SDK and a request id, which mean nothing to them and tell a stranger too much.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0009-message-text-as-enum-templates.md">ADR 0009: message text as enum templates</seealso>
internal static class StreamErrorKinds
{
    /// <summary>The kind of failure this exception was, as the stream says it.</summary>
    public static StreamErrorKind Of(Exception error) => error is ModelReplayMissException
        ? StreamErrorKind.ReplayMiss
        : StreamErrorKind.Api;

    /// <summary>
    /// What the chat shows for this failure: a replay miss says which answer was missing, for the eval runner;
    /// anything else a sorry and a try again.
    /// </summary>
    public static string MessageFor(Exception error) => error is ModelReplayMissException
        ? error.Message
        : MessageText.Format(ApiUserText.TurnFailed);
}
