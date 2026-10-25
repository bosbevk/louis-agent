using System.Runtime.CompilerServices;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Api;

/// <summary>
/// A session client that answers from canned events and never calls the API: the stub eval and the tests. Each
/// <see cref="SendAsync"/> plays the next turn given, in order.
/// </summary>
public sealed class CannedSessionClient(params IReadOnlyList<SessionEvent>[] turns) : ISessionClient
{
    private int _next;
    private readonly List<SessionRequest> _opened = [];

    /// <summary>Every session the client was asked to open, in order, so a test can see what the runner sent (F18 step 4a).</summary>
    public IReadOnlyList<SessionRequest> Opened => _opened;

    public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken)
    {
        _opened.Add(request);
        return Task.FromResult("sess_canned");
    }

    public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var streamEvent in turns[_next++])
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return streamEvent;
        }
        await Task.CompletedTask;
    }

    public Task CloseAsync(string session, CancellationToken cancellationToken) => Task.CompletedTask;
}
