namespace LouisAgent.AcpServer.Protocol;

using System.Text.Json;

/// <summary>Stdout is a protocol (S, CL15, from AcpServer): one JSON-RPC message per line, written under a lock and flushed.</summary>
internal sealed class JsonRpcWriter(TextWriter output)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public Task WriteResultAsync(JsonElement? id, object result, CancellationToken cancellationToken) =>
        WriteAsync(new { jsonrpc = "2.0", id, result }, cancellationToken);

    public Task WriteErrorAsync(JsonElement? id, int code, string message, CancellationToken cancellationToken) =>
        WriteAsync(new { jsonrpc = "2.0", id, error = new { code, message } }, cancellationToken);

    public Task WriteNotificationAsync(string method, object parameters, CancellationToken cancellationToken) =>
        WriteAsync(new { jsonrpc = "2.0", method, @params = parameters }, cancellationToken);

    private async Task WriteAsync(object message, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(message, JsonOptions);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await output.WriteLineAsync(json.AsMemory(), cancellationToken);
            await output.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}
