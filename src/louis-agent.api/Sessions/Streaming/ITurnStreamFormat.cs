using System.Net.ServerSentEvents;
using LouisAgent.Core.Sessions.SubSessions;
using Microsoft.Extensions.AI;

namespace LouisAgent.Api.Sessions.Streaming;

/// <summary>
/// The writer of one turn's events (F18 step 1f). The turn and the session view call only this: a format renders what the
/// turn is in its own shape, and the turn's usage is computed once (<see cref="TurnUsage"/>) and only rendered here.
/// </summary>
public interface ITurnStreamFormat
{
    /// <summary>The turn's start: the toolsets the session loaded and its role.</summary>
    SseItem<string> Start();

    IEnumerable<SseItem<string>> Content(AIContent content);

    IEnumerable<SseItem<string>> Text(string text);

    /// <summary>A sub-session's event, nested under the call that opened it (F5).</summary>
    SseItem<string> SubSession(SubSessionEvent sub);

    /// <summary>The turn's end: its stop reason, and its usage when one was recorded (null otherwise).</summary>
    IEnumerable<SseItem<string>> Stop(string stopReason, TurnUsage? usage);

    /// <summary>A failed turn: the error, with the usage of the requests made before the failure when there are any.</summary>
    IEnumerable<SseItem<string>> Fail(string error, TurnUsage? usage);

    /// <summary>The usage as this format shows it, or null when there is none.</summary>
    object? Usage(TurnUsage? usage);
}
