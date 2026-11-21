using System.Net;
using System.Net.Sockets;

namespace LouisAgent.Core.Tests.TestHttp;

/// <summary>
/// A tiny HTTP server for tests (F52): it listens on a free local port, keeps the headers of every request it gets and
/// answers 200 with an empty body, so a test can see what a real <see cref="HttpClient"/> put on the wire.
/// </summary>
/// <remarks>
/// A Fake (Meszaros's test doubles), not a handler stub: .NET writes the trace header in the handler a real
/// <see cref="HttpClient"/> sends through, which a custom <see cref="HttpMessageHandler"/> would bypass.
/// </remarks>
public sealed class HeaderEcho : IDisposable
{
    /// <summary>The listener on its free port.</summary>
    private readonly HttpListener _listener = new();

    /// <summary>Guards the requests, which the listening loop adds to while a test reads them.</summary>
    private readonly Lock _lock = new();

    /// <summary>Each request's headers, in order.</summary>
    private readonly List<IReadOnlyDictionary<string, string>> _received = [];

    /// <summary>Starts listening on a free port of 127.0.0.1.</summary>
    public HeaderEcho()
    {
        Url = new Uri($"http://127.0.0.1:{FreePort()}/");
        _listener.Prefixes.Add(Url.ToString());
        _listener.Start();
        _ = ListenAsync();
    }

    /// <summary>Where to send.</summary>
    public Uri Url { get; }

    /// <summary>Every request's headers, by lower-case name.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> Received
    {
        get
        {
            lock (_lock) return [.. _received];
        }
    }

    /// <summary>Stops listening.</summary>
    public void Dispose() => _listener.Close();

    /// <summary>Keeps each request's headers and answers 200 until the listener closes.</summary>
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

            var headers = context.Request.Headers;
            var kept = headers.AllKeys.OfType<string>()
                .ToDictionary(name => name.ToLowerInvariant(), name => headers[name] ?? "");
            lock (_lock) _received.Add(kept);
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
