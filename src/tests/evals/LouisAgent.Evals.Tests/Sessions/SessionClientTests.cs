using System.Net;
using System.Text;
using LouisAgent.Evals.Sessions;
using LouisAgent.SessionStream;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Evals.Tests.Sessions;

/// <summary>
/// The client talks to the sessions API as any app does: it opens a session with the request it was given, sends a
/// message and reads its stream, closes the session, and says why when the API says no.
/// </summary>
public class SessionClientTests
{
    private const string Api = "http://api.test/";

    /// <summary>The request goes to the sessions route as it was given, and the session's id comes back.</summary>
    [Test]
    public async Task OpenAsync_PostsTheRequest_AndHandsBackTheSessionsId()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.Created,
            """{"session_id":"s-42","created_at":"2026-11-23T04:30:12Z","toolsets":[],"depth":0}"""));
        var client = Client(handler);
        var request = new CreateSessionRequest(null, "20261123-043012-guide", null, Role: "window",
            Identity: "louis-agent-guide");

        string session = await client.OpenAsync(request);

        Assert.That(session, Is.EqualTo("s-42"));
        var (method, url, body) = handler.Requests.Single();
        Assert.That((method, url), Is.EqualTo((HttpMethod.Post, $"{Api}v1/sessions")));
        Assert.That(body, Does.Contain("\"identity\":\"louis-agent-guide\"").And.Contain("\"role\":\"window\"")
            .And.Contain("\"run\":\"20261123-043012-guide\""));
    }

    /// <summary>The message goes to the session's messages route, and its stream comes back event by event.</summary>
    [Test]
    public async Task SendAsync_PostsTheMessage_AndGivesTheStreamBackEventByEvent()
    {
        const string stream =
            "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"session_id\":\"s-42\"}}\n\n" +
            "event: content_block_delta\n" +
            "data: {\"type\":\"content_block_delta\",\"index\":0," +
            "\"delta\":{\"type\":\"text_delta\",\"text\":\"Hi.\"}}\n\n" +
            "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n";
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(stream, Encoding.UTF8, "text/event-stream")
        });
        var client = Client(handler);

        var events = new List<SessionEvent>();
        await foreach (var streamEvent in client.SendAsync("s-42", "What is louis-agent?")) events.Add(streamEvent);

        Assert.That(events.Select(e => e.GetType()),
            Is.EqualTo(new[] { typeof(MessageStarted), typeof(ContentBlockDelta), typeof(MessageStopped) }));
        var (method, url, body) = handler.Requests.Single();
        Assert.That((method, url), Is.EqualTo((HttpMethod.Post, $"{Api}v1/sessions/s-42/messages")));
        Assert.That(body, Does.Contain("\"message\":\"What is louis-agent?\""));
    }

    /// <summary>Closing a session deletes it.</summary>
    [Test]
    public async Task CloseAsync_DeletesTheSession()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.OK, """{"session_id":"s-42","deleted":true}"""));
        var client = Client(handler);

        await client.CloseAsync("s-42");

        var (method, url, _) = handler.Requests.Single();
        Assert.That((method, url), Is.EqualTo((HttpMethod.Delete, $"{Api}v1/sessions/s-42")));
    }

    /// <summary>When the API says no, the exception carries its status and the API's own reason.</summary>
    [Test]
    public void ARefusal_IsASessionRefusedException_WithTheStatusAndTheApisReason()
    {
        var handler = new StubHandler(_ => Json(HttpStatusCode.BadRequest,
            """{"type":"error","error":{"type":"invalid_request_error",""" +
            """
            "message":"No identity is named 'nobody'."}}
            """));
        var client = Client(handler);
        var request = new CreateSessionRequest(null, null, null, Identity: "nobody");

        var refused = Assert.ThrowsAsync<SessionRefusedException>(() => client.OpenAsync(request));

        Assert.That(refused!.Status, Is.EqualTo(400));
        Assert.That(refused.Reason, Is.EqualTo("No identity is named 'nobody'."));
        Assert.That(refused.Message, Does.Contain("400").And.Contain("No identity is named 'nobody'."));
    }

    /// <summary>A refusal whose body isn't the API's error shape still says what came back.</summary>
    [Test]
    public void ARefusalWithAPlainBody_GivesTheBodyAsTheReason()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("upstream is down")
        });
        var client = Client(handler);

        var refused = Assert.ThrowsAsync<SessionRefusedException>(() => client.CloseAsync("s-42"));

        Assert.That(refused!.Reason, Is.EqualTo("upstream is down"));
    }

    /// <summary>The client over the test's handler, at the API's address.</summary>
    private static SessionClient Client(StubHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri(Api) });

    /// <summary>A JSON answer with this status.</summary>
    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
}
