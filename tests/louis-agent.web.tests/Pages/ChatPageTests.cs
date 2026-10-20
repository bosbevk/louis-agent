using LouisAgent.Web.Git;
using LouisAgent.Web.Pages;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Text;
using LouisAgent.Web.Shared.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Pages;

// T1b: the code-review app's chat page rendered end to end against the stub API: the sidebar's four tabs, the boot
// lines, the composer; then a message, which opens a session and streams the recorded turn. The empty page and the
// page with the turn are pinned to ChatPage.empty.html and ChatPage.turn.html, the proof W2's split changes nothing.
// The page's JS (louisChat.*, localStorage) runs through bUnit's loose interop, which answers every call with nothing;
// the test lists the calls it made, so a new one is a review point.
public class ChatPageTests
{
    private static readonly string[] StartUpCalls =
    [
        // The attachment picker is Blazor's InputFile, which registers itself through JS on its first render.
        "Blazor._internal.InputFile.init",
        "localStorage.getItem", "louisChat.getTheme",
    ];

    private static readonly string[] TurnCalls =
    [
        "louisChat.scrollToBottom", "louisChat.highlightBlocks", "localStorage.setItem",
    ];

    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings);
        var git = new GitState(api);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(new ChatStore(ctx.JSInterop.JSRuntime, settings));
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(git);
        ctx.Services.AddSingleton(new GitHistory(api, git));
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        // The theme switcher reads css/themes/themes.json on its own client.
        ctx.Services.AddSingleton(new HttpClient(new StubHandler(FixtureApi.Answer)) { BaseAddress = new Uri("http://localhost/") });
        return ctx;
    }

    private static string[] Calls(BunitContext ctx) => ctx.JSInterop.Invocations.Select(i => i.Identifier).Distinct().Order(StringComparer.Ordinal).ToArray();

    [Test]
    public void TheEmptyPage_MatchesTheGoldenFile_AndMakesTheseJsCalls()
    {
        using var ctx = Context();

        var cut = ctx.Render<ChatPage>();
        cut.WaitForState(() => cut.FindAll("select").Count > 0, TimeSpan.FromSeconds(10));

        Assert.That(cut.Find(".empty h1").TextContent, Is.EqualTo("LOUIS AGENT"));
        Assert.That(cut.FindAll(".tabs button"), Has.Count.EqualTo(4));
        Assert.That(Calls(ctx), Is.EquivalentTo(StartUpCalls));
        Snapshots.Match("louis-agent.web.tests", "Pages/ChatPage.empty.html", cut.Markup);
    }

    [Test]
    public void AMessage_OpensASession_StreamsTheTurn_AndMatchesTheGoldenFile()
    {
        using var ctx = Context();
        var cut = ctx.Render<ChatPage>();
        cut.WaitForState(() => cut.FindAll("select").Count > 0, TimeSpan.FromSeconds(10));

        cut.Find("#composer-input").Input(SessionStreamFixture.Question);
        cut.Find("#send-button").Click();
        // The turn is over when the Stop button has given way to Send again and the chat was saved to localStorage: the
        // page redraws before the save finishes (a git refresh lands first in the code-review app), so both are awaited.
        cut.WaitForState(() => cut.FindAll(".turn").Count > 0 && cut.FindAll("button.stop").Count == 0
            && ctx.JSInterop.Invocations.Any(i => i.Identifier == "localStorage.setItem"), TimeSpan.FromSeconds(30));

        Assert.That(cut.FindAll(".turn"), Has.Count.EqualTo(1));
        Assert.That(cut.Find(".user-text").TextContent, Is.EqualTo(SessionStreamFixture.Question));
        Assert.That(cut.Find(".chat-usage").TextContent, Does.StartWith("This chat:"));
        Assert.That(Calls(ctx), Is.EquivalentTo(StartUpCalls.Concat(TurnCalls)));
        Snapshots.Match("louis-agent.web.tests", "Pages/ChatPage.turn.html", cut.Markup);
    }
}
