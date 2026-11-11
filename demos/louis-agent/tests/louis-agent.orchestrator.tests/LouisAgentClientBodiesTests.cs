using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Usage.Records;
using LouisAgent.Orchestrator.Client;
using LouisAgent.SessionStream.Wire.Requests;
using LouisAgent.Testing;

namespace LouisAgent.Orchestrator.Tests;

/// <summary>
/// The orchestrator opens a fix session and sends it the task with the session wire's shared records (F127.3.1 step
/// 2): what the API is sent is exactly what the record writes, as core and the browser send it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public class LouisAgentClientBodiesTests
{
    /// <summary>An API that opens sess_1, answers a message with an empty stream, and keeps what it was sent.</summary>
    private static StubHandler Api() => new(request =>
        request.RequestUri!.AbsolutePath == "/v1/sessions"
            ? new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("""{"session_id":"sess_1"}""", Encoding.UTF8, "application/json")
            }
            : new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "text/event-stream")
            });

    /// <summary>The client on that API, with no key.</summary>
    private static LouisAgentClient ClientOn(StubHandler api) =>
        new(new HttpClient(api) { BaseAddress = new Uri("http://louis-agent/") }, null);

    /// <summary>A fix session's opening is the shared record: its tags and toolsets, every other field null.</summary>
    [Test]
    public async Task AFixSessionsOpening_IsTheSharedRecord()
    {
        var api = Api();

        await ClientOn(api).CreateSessionAsync(new UsageTags("fix:1", "run-1", "order-service"),
            toolsets: ["alpha"]);

        Assert.That(api.Requests.Single().Body, Is.EqualTo(JsonSerializer.Serialize(
            new CreateSessionRequest("fix:1", "run-1", "order-service", ["alpha"]), JsonSerializerOptions.Web)));
    }

    /// <summary>The task goes as the shared message record, with no attachments and no page.</summary>
    [Test]
    public async Task ATask_IsTheSharedMessageRecord()
    {
        var api = Api();

        await ClientOn(api).SendAsync("sess_1", "Fix order 1006.");

        Assert.That(api.Requests.Single().Body, Is.EqualTo(JsonSerializer.Serialize(
            new SendMessageRequest("Fix order 1006.", null), JsonSerializerOptions.Web)));
    }
}
