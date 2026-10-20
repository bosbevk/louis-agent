using System.Text.Json;
using LouisAgent.AcpServer.Protocol;

namespace LouisAgent.AcpServer.Tests.Protocol;

// S (CL15): stdout is a protocol: one JSON-RPC message per line, camelCase, a result or an error for a request's id, a
// notification with no id, each flushed as it is written.
public class JsonRpcWriterTests
{
    private static JsonElement Id(int id) => JsonDocument.Parse(id.ToString()).RootElement;

    [Test]
    public async Task AResult_AnError_AndANotification_EachOnOneLine()
    {
        var output = new StringWriter();
        var writer = new JsonRpcWriter(output);

        await writer.WriteResultAsync(Id(1), new { sessionId = "sess_1" }, CancellationToken.None);
        await writer.WriteErrorAsync(Id(2), -32601, "Method not found: x", CancellationToken.None);
        await writer.WriteNotificationAsync("session/update", new { sessionId = "sess_1", update = new { sessionUpdate = "agent_message_chunk" } }, CancellationToken.None);

        Assert.That(output.ToString().ReplaceLineEndings("\n"), Is.EqualTo(
            """{"jsonrpc":"2.0","id":1,"result":{"sessionId":"sess_1"}}""" + "\n" +
            """{"jsonrpc":"2.0","id":2,"error":{"code":-32601,"message":"Method not found: x"}}""" + "\n" +
            """{"jsonrpc":"2.0","method":"session/update","params":{"sessionId":"sess_1","update":{"sessionUpdate":"agent_message_chunk"}}}""" + "\n"));
    }

    [Test]
    public async Task AParseError_HasNoId()
    {
        var output = new StringWriter();

        await new JsonRpcWriter(output).WriteErrorAsync(null, -32700, "Parse error", CancellationToken.None);

        Assert.That(output.ToString().TrimEnd(), Is.EqualTo("""{"jsonrpc":"2.0","id":null,"error":{"code":-32700,"message":"Parse error"}}"""));
    }
}
