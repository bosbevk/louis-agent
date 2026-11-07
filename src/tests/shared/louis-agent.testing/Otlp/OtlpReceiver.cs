using System.Net;
using System.Net.Sockets;
using System.Text;

namespace LouisAgent.Testing.Otlp;

/// <summary>
/// A tiny OTLP collector for tests (F52): it listens on a free local port, keeps the body and path of every POST it
/// gets (OTLP over HTTP sends protobuf to <c>/v1/logs</c> and <c>/v1/traces</c>) and answers 200. No protobuf
/// library: a test looks for UTF-8 strings in the bytes (a service name, a property's name), which is enough to prove
/// a line arrived with what it should carry and nothing else.
/// </summary>
/// <remarks>
/// A Fake (Meszaros's test doubles): a real HTTP endpoint the exporter can't tell from a collector, so the test runs
/// the sink's own code from batching to the wire, platform-neutral, with no container or dashboard to start.
/// </remarks>
public sealed class OtlpReceiver : IDisposable
{
    /// <summary>The listener on its free port.</summary>
    private readonly HttpListener _listener = new();

    /// <summary>Guards the requests, which the listening loop adds to while a test reads them.</summary>
    private readonly Lock _lock = new();

    /// <summary>What came in so far, in order.</summary>
    private readonly List<(string Path, byte[] Body)> _received = [];

    /// <summary>Starts listening on a free port of 127.0.0.1.</summary>
    public OtlpReceiver()
    {
        Endpoint = $"http://127.0.0.1:{FreePort()}";
        _listener.Prefixes.Add(Endpoint + "/");
        _listener.Start();
        _ = ListenAsync();
    }

    /// <summary>The base address an exporter is pointed at, without a path.</summary>
    public string Endpoint { get; }

    /// <summary>Every request's path and body, as they came.</summary>
    public IReadOnlyList<(string Path, byte[] Body)> Received
    {
        get
        {
            lock (_lock) return [.. _received];
        }
    }

    /// <summary>Whether any body that came in holds this text, as UTF-8.</summary>
    public bool Holds(string text)
    {
        byte[] wanted = Encoding.UTF8.GetBytes(text);
        return Received.Any(r => r.Body.AsSpan().IndexOf(wanted) >= 0);
    }

    /// <summary>Waits up to two seconds for a body that holds this text, as an exporter sends in batches.</summary>
    public async Task<bool> EventuallyHoldsAsync(string text)
    {
        for (int i = 0; i < 200; i++)
        {
            if (Holds(text)) return true;
            await Task.Delay(10);
        }

        return false;
    }

    /// <summary>Stops listening.</summary>
    public void Dispose() => _listener.Close();

    /// <summary>Keeps each request and answers 200 until the listener closes.</summary>
    private async Task ListenAsync()
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
            {
                // Closed by Dispose: the loop ends.
                return;
            }

            using var body = new MemoryStream();
            await context.Request.InputStream.CopyToAsync(body);
            lock (_lock) _received.Add((context.Request.Url?.AbsolutePath ?? "", body.ToArray()));
            context.Response.StatusCode = 200;
            context.Response.Close();
        }
    }

    /// <summary>A port nothing listens on right now, from the system.</summary>
    private static int FreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();
        return ((IPEndPoint)socket.LocalEndpoint).Port;
    }
}
