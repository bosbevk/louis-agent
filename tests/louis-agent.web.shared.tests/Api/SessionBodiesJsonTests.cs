using System.Net.Http.Json;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Choices;
using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Web.Shared.Tests.Api;

/// <summary>
/// Pins what the web apps send to open a session and to send a message, byte for byte, as the API reads it: the
/// records write what the anonymous objects they replaced wrote.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public class SessionBodiesJsonTests
{
    /// <summary>Opening a session sends the toolsets, the role and where the user is, nulls included.</summary>
    [Test]
    public async Task OpeningASession_SendsItsBody_AsBefore()
    {
        var body = JsonContent.Create(new CreateSessionBody([AskedToolset.Specialists.Wire()], null, null,
            "Africa/Johannesburg"));

        Assert.That(await body.ReadAsStringAsync(), Is.EqualTo(
            """{"toolsets":["specialists"],"role":null,"location":null,"time_zone":"Africa/Johannesburg"}"""));
    }

    /// <summary>A message goes with its attachments, each by name and content.</summary>
    [Test]
    public async Task AMessage_SendsItsBody_AsBefore()
    {
        var body = JsonContent.Create(new MessageBody("Hallo", [new Attachment("lys.txt", "eiers")]));

        Assert.That(await body.ReadAsStringAsync(), Is.EqualTo(
            """{"message":"Hallo","attachments":[{"name":"lys.txt","content":"eiers"}]}"""));
    }
}
