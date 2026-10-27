using LouisAgent.Core.Messages;
using LouisAgent.Evals.Messages;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Api;

/// <summary>
/// The session client of a re-score (F18 step 5b): there is none. A re-score grades transcripts already recorded, so nothing may open a session
/// or send a message; if something does, it is a bug, and it fails loudly instead of spending a model request.
/// </summary>
public sealed class NoSessionClient : ISessionClient
{
    public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
        throw Refused(nameof(ToolsetsAsync));

    public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken) =>
        throw Refused(nameof(OpenAsync));

    public IAsyncEnumerable<SessionEvent>
        SendAsync(string session, string message, CancellationToken cancellationToken) =>
        throw Refused(nameof(SendAsync));

    public Task CloseAsync(string session, CancellationToken cancellationToken) => throw Refused(nameof(CloseAsync));

    private static InvalidOperationException Refused(string call) =>
        new(MessageText.Format(EvalErrorMessage.NoSessionInARescore, new { Call = call }));
}
