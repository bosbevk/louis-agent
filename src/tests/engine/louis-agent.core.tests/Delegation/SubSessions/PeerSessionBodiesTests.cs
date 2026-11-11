using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.SessionStream.Wire.Requests;
using static LouisAgent.Core.Tests.Delegation.FakePeer;

namespace LouisAgent.Core.Tests.Delegation.SubSessions;

/// <summary>
/// A sub-session's requests are the session wire's shared records (F127.3.1 step 1): what the peer is sent is exactly
/// what the record writes, every field the API reads and nothing of its own, so a renamed field fails at compile time.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public class PeerSessionBodiesTests
{
    /// <summary>The opening it sends reads as a <see cref="CreateSessionRequest"/> and writes back the same.</summary>
    [Test]
    public async Task ASubSessionsOpening_IsTheSharedRecord()
    {
        var handler = Peer();

        await ToolsOn(handler).StartSubSession([TestToolset.Web.Wire()], "Find the price.", agent: "coder");

        Assert.That(handler.Requests[0].Body, Is.EqualTo(Rewritten<CreateSessionRequest>(handler.Requests[0].Body)));
    }

    /// <summary>The message it sends reads as a <see cref="SendMessageRequest"/> and writes back the same.</summary>
    [Test]
    public async Task ASubSessionsMessage_IsTheSharedRecord()
    {
        var handler = Peer();

        await ToolsOn(handler).StartSubSession([TestToolset.Web.Wire()], "Find the price.", agent: "coder");

        Assert.That(handler.Requests[1].Body, Is.EqualTo(Rewritten<SendMessageRequest>(handler.Requests[1].Body)));
    }

    /// <summary>A body read as the record, as the API reads it, and written again as the record writes it.</summary>
    private static string Rewritten<T>(string? body) =>
        JsonSerializer.Serialize(JsonSerializer.Deserialize<T>(body!, JsonSerializerOptions.Web),
            JsonSerializerOptions.Web);
}
