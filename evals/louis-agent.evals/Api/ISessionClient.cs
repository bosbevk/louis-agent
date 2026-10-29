using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Api;

/// <summary>
/// The API as an eval sees it (F18 step 2): its toolsets, a session opened and closed, and a message sent with its stream read
/// through the session-stream reader. The HTTP client comes in step 4; <see cref="CannedSessionClient"/> is the test double.
/// </summary>
public interface ISessionClient
{
    /// <summary>GET /v1/toolsets: every toolset the instance has.</summary>
    Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken);

    /// <summary>POST /v1/sessions: returns the session's id.</summary>
    Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken);

    /// <summary>POST /v1/sessions/{id}/messages: the turn's events, as they arrive.</summary>
    IAsyncEnumerable<SessionEvent> SendAsync(string session, string message, CancellationToken cancellationToken);

    Task CloseAsync(string session, CancellationToken cancellationToken);
}
