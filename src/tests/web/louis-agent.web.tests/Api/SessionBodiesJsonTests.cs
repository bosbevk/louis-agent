using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Storage;
using LouisAgent.Choices;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Web.Tests.Api;

/// <summary>
/// Pins what the web apps send to open a session and to send a message, as the API reads it: the opening is the
/// session wire's shared record since F127.3.1, the message still the web's own body, byte for byte as before.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public class SessionBodiesJsonTests
{
    /// <summary>
    /// Opening a session sends the shared <see cref="CreateSessionRequest"/>: the toolsets, the role, where the user is
    /// and who answers, every other field the record has, null.
    /// </summary>
    [Test]
    public async Task OpeningASession_SendsTheSharedRecord()
    {
        // An API that opens sess_1 and keeps the body it was sent.
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = new StringContent("""{"session_id":"sess_1"}""", Encoding.UTF8, "application/json")
        });
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://api.test/") },
            new ChatSettings());

        await api.CreateSessionAsync([AskedToolset.Agents.Wire()], timeZone: "Africa/Johannesburg",
            identity: "alpha-guide");

        Assert.That(handler.Requests.Single().Body, Is.EqualTo(JsonSerializer.Serialize(
            new CreateSessionRequest(null, null, null, [AskedToolset.Agents.Wire()],
                TimeZone: "Africa/Johannesburg", Identity: "alpha-guide"), JsonSerializerOptions.Web)));
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
