using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;

namespace LouisAgent.SessionStream;

public static class SessionStreamReader
{
    /// <summary>Reads a session's stream event by event. The BCL frames it; this only maps each item.</summary>
    public static async IAsyncEnumerable<SessionEvent> ReadAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
        {
            yield return SessionEventMapper.Map(item.EventType, item.Data);
        }
    }
}
