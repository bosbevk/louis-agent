using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LouisAgent.Evals.Tests.Api;

/// <summary>
/// A real HTTP server on a loopback socket that answers one request with an event stream on a schedule, then goes silent with the connection
/// open. A stub handler can't show a stall: only a real socket has headers that arrived and a body that does not (F18 step 5.1 review).
/// </summary>
internal sealed class SlowSseServer : IAsyncDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly CancellationTokenSource _stop = new();
    private readonly Task _serving;

    /// <param name="events">What to send: each event's text, and how long to wait before sending it.</param>
    public SlowSseServer(params (TimeSpan Wait, string Event)[] events)
    {
        _listener.Start();
        _serving = ServeAsync(events);
    }

    public Uri Address => new($"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}/");

    /// <summary>One event as the session's stream sends it.</summary>
    public static string Event(string text) =>
        "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"" + text + "\"}}\n\n";

    private async Task ServeAsync((TimeSpan Wait, string Event)[] events)
    {
        try
        {
            using var client = await _listener.AcceptTcpClientAsync(_stop.Token);
            var stream = client.GetStream();

            var request = new StringBuilder();
            var one = new byte[1];
            while (!request.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal))
            {
                if (await stream.ReadAsync(one, _stop.Token) == 0) return;
                request.Append((char)one[0]);
            }

            await WriteAsync(stream, "HTTP/1.1 200 OK\r\nContent-Type: text/event-stream\r\nTransfer-Encoding: chunked\r\n\r\n");
            foreach (var (wait, text) in events)
            {
                await Task.Delay(wait, _stop.Token);
                await WriteAsync(stream, $"{Encoding.UTF8.GetByteCount(text):x}\r\n{text}\r\n");
            }
            await Task.Delay(Timeout.Infinite, _stop.Token);   // silence, with the connection open
        }
        catch (Exception error) when (error is OperationCanceledException or IOException or SocketException or ObjectDisposedException)
        {
            // The test is over.
        }
    }

    private async Task WriteAsync(NetworkStream stream, string text)
    {
        await stream.WriteAsync(Encoding.UTF8.GetBytes(text), _stop.Token);
        await stream.FlushAsync(_stop.Token);
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        _listener.Stop();
        await _serving;
        _stop.Dispose();
    }
}
