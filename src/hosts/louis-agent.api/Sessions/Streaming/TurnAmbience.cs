using LouisAgent.Composition.Sessions;
using LouisAgent.Core.Delegation;
using LouisAgent.Core.Sessions;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.SubSessions;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Api.Sessions.Streaming;

/// <summary>
/// The ambient values one step of a session's turn runs inside: its usage scope, its attachments, its tools, the
/// visitor's place, who it runs as (F127.2), its replay, its sub-sessions and the observer of their events. Entered
/// around each step of the engine's stream, where the model requests and the tools run.
/// </summary>
/// <remarks>
/// One scope for eight, so the turn stream reads as the turn and not as its plumbing: each value is an
/// <see cref="AsyncLocal{T}"/> set when entered and put back when left, in reverse, as nested <c>using</c>s did.
/// Entered synchronously, since an async method would put its <see cref="AsyncLocal{T}"/> values back on return.
/// </remarks>
internal sealed class TurnAmbience : IDisposable
{
    private readonly IDisposable[] _scopes;

    private TurnAmbience(IDisposable[] scopes) => _scopes = scopes;

    /// <summary>Enters the session's ambient values for one step of its turn, in order.</summary>
    public static TurnAmbience Enter(AgentSession session, UsageScope usage, Action<SubSessionEvent> observer) =>
        new([
            usage.Activate(),
            SessionAttachments.Use(session.Attachments),
            AgentTools.Use(session.Engine.Tools.Select(t => t.Name)),
            UserPlace.Use(session.Place),
            TurnIdentity.Use(session.Who),
            SessionReplay.Use(session.Replay),
            SubSessionRegistry.Use(session.SubSessions),
            SubSessionFeed.Observe(observer)
        ]);

    /// <summary>Leaves them, the last entered first, so each puts back the value before it.</summary>
    public void Dispose()
    {
        for (int i = _scopes.Length - 1; i >= 0; i--) _scopes[i].Dispose();
    }
}
