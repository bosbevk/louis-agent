using System.IO.Pipelines;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace LouisAgent.Mcp.Tests;

/// <summary>
/// A real MCP server and a real MCP client joined by two in-memory pipes, for a test that talks the protocol without a
/// port or a process (F56, finding C1).
/// </summary>
/// <remarks>
/// Test Helper (Meszaros, xUnit Test Patterns): the server is the SDK's own, set up with the options a test gives it,
/// so what passes here is what a client on stdio or HTTP gets. Disposing it closes the client, then stops the server.
/// </remarks>
internal sealed class InMemoryMcp(McpClient client, Task serverRun, CancellationTokenSource stop) : IAsyncDisposable
{
    /// <summary>The client, already through <c>initialize</c>.</summary>
    public McpClient Client => client;

    /// <summary>Starts a server with these options and connects a client to it.</summary>
    public static async Task<InMemoryMcp> StartAsync(McpServerOptions options)
    {
        // One pipe each way: what the client writes the server reads, and back.
        var toServer = new Pipe();
        var toClient = new Pipe();
        var server = ModelContextProtocol.Server.McpServer.Create(
            new StreamServerTransport(toServer.Reader.AsStream(), toClient.Writer.AsStream()), options);
        var stop = new CancellationTokenSource();
        var run = server.RunAsync(stop.Token);
        var connected = await McpClient.CreateAsync(
            new StreamClientTransport(toServer.Writer.AsStream(), toClient.Reader.AsStream()));
        return new InMemoryMcp(connected, run, stop);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await client.DisposeAsync();
        await stop.CancelAsync();
        // A cancelled run ends in OperationCanceledException; the server has stopped either way.
        await serverRun.ContinueWith(_ => { }, TaskScheduler.Default);
        stop.Dispose();
    }
}
