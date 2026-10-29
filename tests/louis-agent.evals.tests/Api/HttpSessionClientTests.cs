using LouisAgent.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Evals.Api;
using LouisAgent.SessionStream;

namespace LouisAgent.Evals.Tests.Api;

// F18 step 5.1: the HTTP session client, as benchmarks/puls_check/client.py calls the API. Written first: the client threw
// NotImplementedException on the red run. The API is a stub handler, so no network and no model.
public class HttpSessionClientTests
{
    private static readonly EvalMode Mode = new(SampleMode.Beta, "puls/voice-af",
        [SampleToolset.Gamma, SampleToolset.Alpha]);

    private static SessionRequest Request(string? place = "Malmesbury, Wes-Kaap, Suid-Afrika",
        WebReplayRequest? replay = null) =>
        new(Mode, place, "Africa/Johannesburg", "cook-malmesbury", 2, "run_7:puls-cook:2",
            replay ?? new WebReplayRequest(WebReplayMode.Record, null));

    private static (HttpSessionClient Client, StubHandler Handler) ClientOver(
        Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        return (new HttpSessionClient(HttpSessionClient.CreateHttp(null, null, handler)), handler);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static async Task<JsonElement> OpenBody(SessionRequest request)
    {
        var (client, handler) = ClientOver(_ => Json("""{"session_id":"sess_1"}"""));
        await client.OpenAsync(request, CancellationToken.None);
        return JsonDocument.Parse(handler.Requests.Single().Body!).RootElement;
    }

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

    [Test]
    public async Task ANullPlaceIsLeftOut_SoTheApisDefaultsHold()
    {
        var body = await OpenBody(Request(place: null));

        Assert.That(body.TryGetProperty("location", out _), Is.False);
        Assert.That(body.GetProperty("time_zone").GetString(), Is.EqualTo("Africa/Johannesburg"));
    }

    [Test]
    public async Task TheReplayIsAlwaysSent_WebIsItsLowerCaseName_AndLeftOutWhenNull()
    {
        var recording =
            (await OpenBody(Request(replay: new WebReplayRequest(WebReplayMode.Record, null)))).GetProperty("replay");
        var nothing = (await OpenBody(Request(replay: new WebReplayRequest(null, null)))).GetProperty("replay");

        Assert.That(recording.GetProperty("web").GetString(), Is.EqualTo("record"));
        Assert.That(nothing.TryGetProperty("web", out _), Is.False, "the instance's WEB_REPLAY decides");
        Assert.That(nothing.GetProperty("model").GetBoolean(), Is.False, "an eval session always sends the mark");
    }

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

        Assert.That(recording.TryGetProperty("of", out _), Is.False);
        Assert.That(replay.GetProperty("of").GetString(), Is.EqualTo(trial));
        Assert.That(replay.GetProperty("model").GetBoolean(), Is.False);
        Assert.That(withModel.GetProperty("model").GetBoolean(), Is.True);
    }

    [Test]
    public async Task TheKeyIsSentWhenSet_AndNotWithout()
    {
        var seen = new List<string?>();

        HttpResponseMessage Record(HttpRequestMessage request)
        {
            seen.Add(request.Headers.TryGetValues("x-api-key", out var values) ? values.Single() : null);
            return Json("""{"toolsets":[]}""");
        }

        await new HttpSessionClient(HttpSessionClient.CreateHttp(null, "secret", new StubHandler(Record)))
            .ToolsetsAsync(CancellationToken.None);
        await new HttpSessionClient(HttpSessionClient.CreateHttp(null, null, new StubHandler(Record))).ToolsetsAsync(
            CancellationToken.None);

        Assert.That(seen, Is.EqualTo(new string?[] { "secret", null }));
    }

    [Test]
    public void TheFactoryDefaultsToPulsOnItsPort_AndWaitsTenMinutesForATurn()
    {
        var http = HttpSessionClient.CreateHttp(null, null);
        var named = HttpSessionClient.CreateHttp("http://puls.test:5070", null);

        Assert.That(http.BaseAddress, Is.EqualTo(new Uri("http://127.0.0.1:5070/")));
        Assert.That(named.BaseAddress, Is.EqualTo(new Uri("http://puls.test:5070/")));
        Assert.That(http.Timeout, Is.EqualTo(TimeSpan.FromMinutes(10)));
    }

    [Test]
    public async Task ToolsetsAreOnlyTheAvailableOnes()
    {
        var (client, handler) = ClientOver(_ => Json(SampleToolset.Fill("""
                                                                        {"default":["skills"],"toolsets":[
                                                                          {"name":"<Alpha>","available":true},{"name":"<Beta>","available":false,"missing_settings":["SHOP_CATALOGUES"]},{"name":"<Gamma>","available":true}]}
                                                                        """)));

        var toolsets = await client.ToolsetsAsync(CancellationToken.None);

        Assert.That(toolsets, Is.EqualTo(new[] { SampleToolset.Alpha, SampleToolset.Gamma }));
        Assert.That((handler.Requests.Single().Method, handler.Requests.Single().Url),
            Is.EqualTo((HttpMethod.Get, "http://127.0.0.1:5070/v1/toolsets")));
    }

    [Test]
    public async Task ASendPostsTheMessage_AndYieldsTheStreamsEventsInOrder()
    {
        const string stream =
            "event: message_start\ndata: {\"type\":\"message_start\",\"message\":{\"session_id\":\"sess_1\"}}\n\n"
            + "event: content_block_delta\ndata: {\"type\":\"content_block_delta\",\"index\":0,\"delta\":{\"type\":\"text_delta\",\"text\":\"Hallo\"}}\n\n"
            + "event: message_stop\ndata: {\"type\":\"message_stop\"}\n\n";
        var (client, handler) = ClientOver(_ => new HttpResponseMessage(HttpStatusCode.OK)
        { Content = new StringContent(stream, Encoding.UTF8, "text/event-stream") });

        var events = new List<SessionEvent>();
        await foreach (var e in client.SendAsync("sess_1", "Gee my 'n resep", CancellationToken.None)) events.Add(e);

        Assert.That(events.Select(e => e.Type),
            Is.EqualTo(new[] { "message_start", "content_block_delta", "message_stop" }));
        var (method, url, body) = handler.Requests.Single();
        Assert.That((method, url), Is.EqualTo((HttpMethod.Post, "http://127.0.0.1:5070/v1/sessions/sess_1/messages")));
        Assert.That(JsonDocument.Parse(body!).RootElement.GetProperty("message").GetString(),
            Is.EqualTo("Gee my 'n resep"));
    }

    [TestCase(HttpStatusCode.BadRequest,
        """{"type":"error","error":{"type":"invalid_request","message":"Unknown role 'x'."}}""", 400,
        "Unknown role 'x'.")]
    [TestCase(HttpStatusCode.InternalServerError,
        """{"type":"error","error":{"type":"api_error","message":"The model is down."}}""", 500, "The model is down.")]
    public void ANon2xxThrowsWithTheApisMessage(HttpStatusCode status, string body, int code, string message)
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

    [Test]
    public void ABodyThatIsNotTheApisErrorShapeStillNamesTheStatus()
    {
        var (client, _) = ClientOver(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        { Content = new StringContent("<html>bad gateway</html>") });

        var error = Assert.ThrowsAsync<EvalApiException>(async () =>
            await client.ToolsetsAsync(CancellationToken.None));

        Assert.That(error!.Status, Is.EqualTo(502));
        Assert.That(error.Message, Does.Contain("502"));
    }

    [Test]
    public async Task CloseDeletesTheSessionById()
    {
        var (client, handler) = ClientOver(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        await client.CloseAsync("sess_9", CancellationToken.None);

        Assert.That((handler.Requests.Single().Method, handler.Requests.Single().Url),
            Is.EqualTo((HttpMethod.Delete, "http://127.0.0.1:5070/v1/sessions/sess_9")));
    }
}
