using System.Runtime.CompilerServices;
using System.Text.Json;
using LouisAgent.Evals.Api;
using LouisAgent.Evals.Runner.Messages;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Runner.Evals.Stub;

/// <summary>
/// A session client that answers the stub's canned turns and never calls the API. Its session ids are the Python stub's
/// (<c>sess_stub_{mode}_{repeat}_{task}</c>), so the protocol's lines match line for line. It ignores the session's replay: the
/// stub reads no web, so a recording of it has nothing to save and a replay nothing to answer from (F18 step 4a).
/// </summary>
public sealed class StubSessionClient : ISessionClient
{
    private readonly Dictionary<string, (EvalMode Mode, int Repeat, string Task, int Turns)> _sessions = new(StringComparer.Ordinal);

    public Task<IReadOnlyList<string>> ToolsetsAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>([]);

    public Task<string> OpenAsync(SessionRequest request, CancellationToken cancellationToken)
    {
        string session = $"sess_stub_{request.Mode.Name}_{request.Repeat}_{request.Task}";
        _sessions[session] = (request.Mode, request.Repeat, request.Task, 0);
        return Task.FromResult(session);
    }

    public async IAsyncEnumerable<SessionEvent> SendAsync(string session, string message,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (mode, repeat, task, turns) = _sessions[session];
        _sessions[session] = (mode, repeat, task, turns + 1);
        bool cook = mode.Toolsets.Contains("specialists", StringComparer.Ordinal);
        foreach (var (type, data) in StubStream.Turn(task, turns + 1, cook))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return SessionEvent.From(type, JsonSerializer.SerializeToElement(data));
        }
        await Task.CompletedTask;
    }

    public Task CloseAsync(string session, CancellationToken cancellationToken)
    {
        _sessions.Remove(session);
        return Task.CompletedTask;
    }
}
