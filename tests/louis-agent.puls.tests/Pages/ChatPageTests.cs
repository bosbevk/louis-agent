using LouisAgent.Web.Testing;
using LouisAgent.Testing;
using LouisAgent.Puls.Pages;
using LouisAgent.Puls.Text;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Puls.Tests.Pages;

// T1: Puls's chat page rendered end to end against the stub API (FixtureApi): the API client is the real one, over a
// stub that answers /v1/model, /v1/roles and /v1/toolsets with fixed JSON, opens one session, and streams the recorded Puls turn
// for its message. The empty page and the page with the turn are pinned to ChatPage.empty.html and ChatPage.turn.html. The
// page's JS (pulsUi.*, louisChat.*, localStorage) runs through bUnit's loose interop, which answers every call with
// nothing; the test lists the calls it made, so a new one is a review point.
public class ChatPageTests
{
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

    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(PulsText.Afrikaans);
        return ctx;
    }

    private static string[] Calls(BunitContext ctx) => ctx.JSInterop.Invocations.Select(i => i.Identifier).Distinct()
        .Order(StringComparer.Ordinal).ToArray();

    [Test]
    public void TheEmptyPage_MatchesTheGoldenFile_AndMakesTheseJsCalls()
    {
        using var ctx = Context();

        var cut = ctx.Render<ChatPage>();
        cut.WaitForState(() => cut.FindAll(".model-name, #composer-input").Count > 0);

        Assert.That(cut.Find("h1").TextContent, Is.EqualTo("Hoe kan ek help?"));
        Assert.That(Calls(ctx), Is.EquivalentTo(StartUpCalls));
        Snapshots.Match("louis-agent.puls.tests", "Pages/ChatPage.empty.html", cut.Markup);
    }

    [Test]
    public void AMessage_OpensASession_StreamsTheTurn_AndMatchesTheGoldenFile()
    {
        using var ctx = Context();
        var cut = ctx.Render<ChatPage>();
        cut.WaitForState(() => cut.FindAll("#composer-input").Count > 0);

        cut.Find("#composer-input").Input(SessionStreamFixture.Question);
        cut.Find("#send-button").Click();
        // The turn is over when the Stop button has given way to Send again and the chat was saved to localStorage: the
        // page redraws before the save finishes (a git refresh lands first in the code-review app), so both are awaited.
        cut.WaitForState(() => cut.FindAll(".turn").Count > 0 && cut.FindAll("button.stop").Count == 0
                                                              && ctx.JSInterop.Invocations.Any(i =>
                                                                  i.Identifier == "localStorage.setItem"),
            TimeSpan.FromSeconds(30));

        Assert.That(cut.FindAll(".turn"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".user-text").TextContent, Is.EqualTo(SessionStreamFixture.Question));
        Assert.That(cut.Find(".delegation-agent strong").TextContent, Is.EqualTo(SessionStreamFixture.CookRole));
        Assert.That(cut.Find(".chat-usage").TextContent, Does.Contain("versoeke"));
        Assert.That(Calls(ctx), Is.EquivalentTo(StartUpCalls.Concat(TurnCalls)));
        Snapshots.Match("louis-agent.puls.tests", "Pages/ChatPage.turn.html", cut.Markup);
    }
}
