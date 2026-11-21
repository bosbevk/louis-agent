using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Evals.Api;
using LouisAgent.SessionStream;
using LouisAgent.SessionStream.Wire.Requests;

namespace LouisAgent.Evals.Tests.Api;

/// <summary>
/// The HTTP session client, as <c>benchmarks/puls_check/client.py</c> called the API (F18 step 5.1). Written first:
/// the client threw <c>NotImplementedException</c> on the red run. The API is a stub handler, so no network and no
/// model.
/// </summary>
public class HttpSessionClientTests
{
    /// <summary>The mode every eval here runs: a voice with two toolsets.</summary>
    private static readonly EvalMode Mode = new(SampleMode.Beta, "puls/voice-af",
        [SampleToolset.Gamma, SampleToolset.Alpha]);

    /// <summary>One scenario's session: its place, time zone, task, repeat, run tag and web.</summary>
    private static SessionRequest Request(string? place = "Malmesbury, Wes-Kaap, Suid-Afrika",
        WebReplayRequest? replay = null) =>
        new(Mode, place, "Africa/Johannesburg", "cook-malmesbury", 2, "run_7:puls-cook:2",
            replay ?? new WebReplayRequest(WebReplayMode.Record, null));

    /// <summary>The client on a stub API that answers as told and keeps what it was sent.</summary>
    private static (HttpSessionClient Client, StubHandler Handler) ClientOver(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        return (new HttpSessionClient(HttpSessionClient.CreateHttp(null, null, handler)), handler);
    }

    /// <summary>A JSON answer with this status.</summary>
    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    /// <summary>The body the client opens this session with.</summary>
    private static async Task<JsonElement> OpenBody(SessionRequest request)
    {
        var (client, handler) = ClientOver(_ => Json("""{"session_id":"sess_1"}"""));
        await client.OpenAsync(request, CancellationToken.None);
        return JsonDocument.Parse(handler.Requests.Single().Body!).RootElement;
    }

    /// <summary>Opening posts the session's settings and hands back the id the API gave.</summary>
    [Test]
    public async Task OpenPostsTheSessionBody_AndReturnsItsId()
    {
        var (client, handler) = ClientOver(_ => Json("""{"session_id":"sess_42"}"""));

        string id = await client.OpenAsync(Request(), CancellationToken.None);

        Assert.That(id, Is.EqualTo("sess_42"));
        var (method, url, text) = handler.Requests.Single();
        Assert.That((method, url), Is.EqualTo((HttpMethod.Post, "http://127.0.0.1:5070/v1/sessions")));
        var body = JsonDocument.Parse(text!).RootElement;
        Assert.That(body.GetProperty("toolsets").EnumerateArray().Select(t => t.GetString()),
            Is.EqualTo(new[] { SampleToolset.Gamma, SampleToolset.Alpha }));
        Assert.That(body.GetProperty("role").GetString(), Is.EqualTo("puls/voice-af"));
        Assert.That(body.GetProperty("time_zone").GetString(), Is.EqualTo("Africa/Johannesburg"));
        Assert.That(body.GetProperty("location").GetString(), Is.EqualTo("Malmesbury, Wes-Kaap, Suid-Afrika"));
        Assert.That(body.GetProperty("task").GetString(), Is.EqualTo("cook-malmesbury"));
        Assert.That(body.GetProperty("run").GetString(), Is.EqualTo("run_7:puls-cook:2"));
    }

    /// <summary>
    /// The opening is the session wire's shared record (F127.3.1 step 2): every field, as core and the browser send
    /// it, the ones this eval leaves empty as null.
    /// </summary>
    [Test]
    public async Task TheOpening_IsTheSharedRecord()
    {
        var (client, handler) = ClientOver(_ => Json("""{"session_id":"sess_1"}"""));

        await client.OpenAsync(Request(), CancellationToken.None);

        Assert.That(handler.Requests.Single().Body, Is.EqualTo(JsonSerializer.Serialize(
            new CreateSessionRequest("cook-malmesbury", "run_7:puls-cook:2", null,
                [SampleToolset.Gamma, SampleToolset.Alpha], Location: "Malmesbury, Wes-Kaap, Suid-Afrika",
                TimeZone: "Africa/Johannesburg", Llm: Mode.Llm, Role: "puls/voice-af",
                Replay: new ReplayRequest("record", null)), JsonSerializerOptions.Web)));
    }

    /// <summary>A question goes as the shared message record, with no attachments and no page.</summary>
    [Test]
    public async Task AQuestion_IsTheSharedMessageRecord()
    {
        var (client, handler) = ClientOver(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("", Encoding.UTF8, "text/event-stream")
        });

        await foreach (var _ in client.SendAsync("sess_1", "Gee my 'n resep", CancellationToken.None))
        {
        }

        Assert.That(handler.Requests.Single().Body, Is.EqualTo(JsonSerializer.Serialize(
            new SendMessageRequest("Gee my 'n resep", null), JsonSerializerOptions.Web)));
    }

    /// <summary>
    /// The mode's depth goes with the session (F106.3.2 step 7): a role's eval opens its session at a specialist's
    /// level, and a voice's eval at 0.
    /// </summary>
    [Test]
    public async Task TheModesDepth_IsSent()
    {
        var role = await OpenBody(Request() with { Mode = Mode with { Depth = 1 } });
        var voice = await OpenBody(Request());

        Assert.That((role.GetProperty("depth").GetInt32(), voice.GetProperty("depth").GetInt32()), Is.EqualTo((1, 0)));
    }

    /// <summary>No place goes as null, so the API's defaults hold, and the time zone still goes.</summary>
    [Test]
    public async Task ANullPlaceIsSentAsNull_SoTheApisDefaultsHold()
    {
        var body = await OpenBody(Request(place: null));

        Assert.That(body.GetProperty("location").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(body.GetProperty("time_zone").GetString(), Is.EqualTo("Africa/Johannesburg"));
    }

    /// <summary>
    /// An eval always sends its <c>replay</c> mark: the web by its wire name, or null to leave it to the instance.
    /// </summary>
    [Test]
    public async Task TheReplayIsAlwaysSent_WebIsItsLowerCaseName_AndNullWhenNone()
    {
        var recording =
            (await OpenBody(Request(replay: new WebReplayRequest(WebReplayMode.Record, null)))).GetProperty("replay");
        var nothing = (await OpenBody(Request(replay: new WebReplayRequest(null, null)))).GetProperty("replay");

        Assert.That(recording.GetProperty("web").GetString(), Is.EqualTo("record"));
        Assert.That(nothing.GetProperty("web").ValueKind, Is.EqualTo(JsonValueKind.Null),
            "the instance's WEB_REPLAY decides");
        Assert.That(nothing.GetProperty("model").GetBoolean(), Is.False, "an eval session always sends the mark");
    }

    /// <summary>The trial goes with a replay only, and the model is replayed only when asked.</summary>
    [Test]
    public async Task OfIsSentOnAReplayOnly_AndModelIsTrueOnlyWhenAsked()
    {
        const string trial = "run_6:puls-cook:2/cook-malmesbury";
        var recording =
            (await OpenBody(Request(replay: new WebReplayRequest(WebReplayMode.Record, null)))).GetProperty("replay");
        var replay =
            (await OpenBody(Request(replay: new WebReplayRequest(WebReplayMode.Replay, trial)))).GetProperty("replay");
        var withModel = (await OpenBody(Request(replay: new WebReplayRequest(WebReplayMode.Replay, trial, true))))
            .GetProperty("replay");

        Assert.That(recording.GetProperty("of").ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(replay.GetProperty("of").GetString(), Is.EqualTo(trial));
        Assert.That(replay.GetProperty("model").GetBoolean(), Is.False);
        Assert.That(withModel.GetProperty("model").GetBoolean(), Is.True);
    }

    /// <summary>The API key goes with every call when there is one, and no header when there isn't.</summary>
    [Test]
    public async Task TheKeyIsSentWhenSet_AndNotWithout()
    {
        var seen = new List<string?>();

        // Notes the key each call carried, and answers with no toolsets.
        HttpResponseMessage Record(HttpRequestMessage request)
        {
            seen.Add(request.Headers.TryGetValues("x-api-key", out var values) ? values.Single() : null);
            return Json("""{"toolsets":[]}""");
        }

        await new HttpSessionClient(HttpSessionClient.CreateHttp(null, "secret", new StubHandler(Record)))
            .ToolsetsAsync(CancellationToken.None);
        await new HttpSessionClient(HttpSessionClient.CreateHttp(null, null, new StubHandler(Record))).ToolsetsAsync(
            CancellationToken.None);

        Assert.That(seen, Is.EqualTo(new[] { "secret", null }));
    }

    /// <summary>With no address the client goes to the local API, and it waits ten minutes for a turn.</summary>
    [Test]
    public void TheFactoryDefaultsToPulsOnItsPort_AndWaitsTenMinutesForATurn()
    {
        var http = HttpSessionClient.CreateHttp(null, null);
        var named = HttpSessionClient.CreateHttp("http://puls.test:5070", null);

        Assert.That(http.BaseAddress, Is.EqualTo(new Uri("http://127.0.0.1:5070/")));
        Assert.That(named.BaseAddress, Is.EqualTo(new Uri("http://puls.test:5070/")));
        Assert.That(http.Timeout, Is.EqualTo(TimeSpan.FromMinutes(10)));
    }

    /// <summary>Of the toolsets the API lists, only the ones this instance has the settings for count.</summary>
    [Test]
    public async Task ToolsetsAreOnlyTheAvailableOnes()
    {
        var (client, handler) = ClientOver(_ => Json(SampleToolset.Fill(
            """
            {"default":["skills"],"toolsets":[
              {"name":"<Alpha>","available":true},
              {"name":"<Beta>","available":false,"missing_settings":["SHOP_CATALOGUES"]},
              {"name":"<Gamma>","available":true}]}
            """)));

        var toolsets = await client.ToolsetsAsync(CancellationToken.None);

        Assert.That(toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Gamma }));
        Assert.That((handler.Requests.Single().Method, handler.Requests.Single().Url),
            Is.EqualTo((HttpMethod.Get, "http://127.0.0.1:5070/v1/toolsets")));
    }

    /// <summary>A question is posted to its session, and the answer's events come back in the order sent.</summary>
    [Test]
    public async Task ASendPostsTheMessage_AndYieldsTheStreamsEventsInOrder()
    {
        const string stream =
            "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"session_id\":\"sess_1\"}}\n\n"
            + "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":0,"
            + "\"delta\":{\"type\":\"text_delta\",\"text\":\"Hallo\"}}\n\n"
            + "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n";
        var (client, handler) = ClientOver(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(stream, Encoding.UTF8, "text/event-stream")
        });

        var events = new List<SessionEvent>();
        await foreach (var e in client.SendAsync("sess_1", "Gee my 'n resep", CancellationToken.None)) events.Add(e);

        Assert.That(events.Select(e => e.Type),
            Is.EqualTo(new[] { "message_start", "content_block_delta", "message_stop" }));
        var (method, url, body) = handler.Requests.Single();
        Assert.That((method, url), Is.EqualTo((HttpMethod.Post, "http://127.0.0.1:5070/v1/sessions/sess_1/messages")));
        Assert.That(JsonDocument.Parse(body!).RootElement.GetProperty("message").GetString(),
            Is.EqualTo("Gee my 'n resep"));
    }

    /// <summary>An error from the API, opening or sending, throws with its status and the API's own words.</summary>
    [TestCase(HttpStatusCode.BadRequest,
        """{"type":"error","error":{"type":"invalid_request","message":"Unknown role 'x'."}}""", 400,
        "Unknown role 'x'.")]
    [TestCase(HttpStatusCode.InternalServerError,
        """{"type":"error","error":{"type":"api_error","message":"The model is down."}}""", 500, "The model is down.")]
    public void AnErrorStatusThrowsWithTheApisMessage(HttpStatusCode status, string body, int code, string message)
    {
        var (client, _) = ClientOver(_ => Json(body, status));

        var open = Assert.ThrowsAsync<EvalApiException>(async () =>
            await client.OpenAsync(Request(), CancellationToken.None));
        var send = Assert.ThrowsAsync<EvalApiException>(async () =>
        {
            await foreach (var _ in client.SendAsync("sess_1", "Hallo", CancellationToken.None))
            {
            }
        });

        Assert.That(open!.Status, Is.EqualTo(code));
        Assert.That(open.Message, Does.Contain(message));
        Assert.That((send!.Status, send.Message), Is.EqualTo((open.Status, open.Message)));
    }

    /// <summary>An error page that isn't the API's (a gateway's HTML) still names its status.</summary>
    [Test]
    public void ABodyThatIsNotTheApisErrorShapeStillNamesTheStatus()
    {
        var (client, _) = ClientOver(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("<html>bad gateway</html>")
        });

        var error = Assert.ThrowsAsync<EvalApiException>(async () =>
            await client.ToolsetsAsync(CancellationToken.None));

        Assert.That(error!.Status, Is.EqualTo(502));
        Assert.That(error.Message, Does.Contain("502"));
    }

    /// <summary>Closing deletes the session by its id.</summary>
    [Test]
    public async Task CloseDeletesTheSessionById()
    {
        var (client, handler) = ClientOver(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await client.CloseAsync("sess_9", CancellationToken.None);

        Assert.That((handler.Requests.Single().Method, handler.Requests.Single().Url),
            Is.EqualTo((HttpMethod.Delete, "http://127.0.0.1:5070/v1/sessions/sess_9")));
    }
}
