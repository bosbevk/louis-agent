using System.Net;
using System.Net.Http.Headers;
using LouisAgent.Puls.Pages;
using LouisAgent.Puls.Tests.Fixtures;
using LouisAgent.Puls.Text;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Pages;

// T1: Puls's chat page rendered end to end against a fake API: the API client is the real one, over a stub that
// answers /model, /roles and /toolsets with fixed JSON, opens one session, and streams the recorded Puls turn for its
// message. The empty page and the page with the turn are pinned to ChatPage.empty.html and ChatPage.turn.html. The
// page's JS (pulsUi.*, louisChat.*, localStorage) runs through bUnit's loose interop, which answers every call with
// nothing; the test lists the calls it made, so a new one is a review point.
public class ChatPageTests
{
    private const string SessionId = "sess_fixture";

    private static readonly string[] StartUpCalls =
    [
        // The attachment picker is Blazor's InputFile, which registers itself through JS on its first render.
        "Blazor._internal.InputFile.init",
        "localStorage.getItem", "pulsUi.loadLocation", "pulsUi.motion", "pulsUi.loadLanguage", "pulsUi.reducedMotion",
        "pulsUi.countCosts",
    ];

    private static readonly string[] TurnCalls =
    [
        "pulsUi.timeZone", "louisChat.scrollToBottom", "louisChat.highlightBlocks", "localStorage.setItem",
    ];

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };

    private static HttpResponseMessage Answer(HttpRequestMessage request)
    {
        string path = request.RequestUri!.AbsolutePath;
        if (request.Method == HttpMethod.Get && path == "/model") return Json("""{"provider":"anthropic","model":"claude-haiku-5-5"}""");
        if (request.Method == HttpMethod.Get && path == "/roles") return Json("""{"default":"louis-agent/voice","agent":["louis-agent/voice","puls/voice-af","puls/voice-en"],"specialist":["puls-cook"],"minion":[]}""");
        if (request.Method == HttpMethod.Get && path == "/toolsets") return Json("""{"default":["skills"],"toolsets":[{"name":"specialists","available":true},{"name":"web","available":true},{"name":"shops","available":true}]}""");
        if (request.Method == HttpMethod.Post && path == "/sessions") return Json($$"""{"session_id":"{{SessionId}}","toolsets":["specialists","web"]}""");
        if (request.Method == HttpMethod.Post && path == $"/sessions/{SessionId}/messages")
        {
            var content = new StreamContent(File.OpenRead(SessionStreamFixture.StreamFile));
            content.Headers.ContentType = new MediaTypeHeaderValue("text/event-stream");
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = content };
        }
        // No rates on this server: costs show as charged, the switcher stays hidden.
        return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("""{"type":"error","error":{"type":"not_found_error","message":"no"}}""") };
    }

    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = new AgentApi(new HttpClient(new StubHandler(Answer)) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(PulsText.Afrikaans);
        return ctx;
    }

    private static string[] Calls(BunitContext ctx) => ctx.JSInterop.Invocations.Select(i => i.Identifier).Distinct().Order(StringComparer.Ordinal).ToArray();

    [Test]
    public void TheEmptyPage_MatchesTheGoldenFile_AndMakesTheseJsCalls()
    {
        using var ctx = Context();

        var cut = ctx.Render<ChatPage>();
        cut.WaitForState(() => cut.FindAll(".model-name, #composer-input").Count > 0);

        Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Hoe kan ek help?"));
        Assert.That(Calls(ctx), Is.EquivalentTo(StartUpCalls));
        Snapshots.Match("Pages/ChatPage.empty.html", cut.Markup);
    }

    [Test]
    public void AMessage_OpensASession_StreamsTheTurn_AndMatchesTheGoldenFile()
    {
        using var ctx = Context();
        var cut = ctx.Render<ChatPage>();
        cut.WaitForState(() => cut.FindAll("#composer-input").Count > 0);

        cut.Find("#composer-input").Input(SessionStreamFixture.Question);
        cut.Find("#send-button").Click();
        cut.WaitForState(() => cut.FindAll(".chat-usage").Count > 0, TimeSpan.FromSeconds(30));

        Assert.That(cut.FindAll(".turn"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".user-text").TextContent, Is.EqualTo(SessionStreamFixture.Question));
        Assert.That(cut.Find(".delegation-agent strong").TextContent, Is.EqualTo("puls-cook"));
        Assert.That(cut.Find(".chat-usage").TextContent, Does.Contain("versoeke"));
        Assert.That(Calls(ctx), Is.EquivalentTo(StartUpCalls.Concat(TurnCalls)));
        Snapshots.Match("Pages/ChatPage.turn.html", cut.Markup);
    }
}
