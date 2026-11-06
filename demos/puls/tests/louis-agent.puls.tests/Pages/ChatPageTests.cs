using LouisAgent.Web.Testing;
using LouisAgent.Testing;
using LouisAgent.Puls.Pages;
using LouisAgent.Puls.Text;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;
using LouisAgent.Web.Api;

namespace LouisAgent.Puls.Tests.Pages;

/// <summary>
/// Puls's chat page rendered end to end against the stub API: the API client is the real one, over a stub that answers
/// the model, roles and toolsets with fixed JSON, opens one session, and streams the recorded turn for its message. The
/// empty page and the page with the turn are pinned to golden files. The page's JS runs through bUnit's loose interop,
/// which answers every call with nothing; the test lists the calls it made, so a new one is a review point.
/// </summary>
public class ChatPageTests
{
    private static readonly string[] StartUpCalls =
    [
        // The attachment picker is Blazor's InputFile, which registers itself through JS on its first render.
        "Blazor._internal.InputFile.init",
        "localStorage.getItem", "chatUi.loadLocation", "chatUi.motion", "chatUi.loadLanguage", "chatUi.reducedMotion",
        "chatUi.countCosts",
    ];

    private static readonly string[] TurnCalls =
    [
        "chatUi.timeZone", "louisChat.scrollToBottom", "louisChat.highlightBlocks", "localStorage.setItem",
    ];

    /// <summary>A bUnit context with everything the page asks for, over the stub API.</summary>
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

    /// <summary>Which scripts the page has called, each once, in order of name.</summary>
    private static string[] Calls(BunitContext ctx) => ctx.JSInterop.Invocations.Select(i => i.Identifier).Distinct()
        .Order(StringComparer.Ordinal).ToArray();

    /// <summary>The empty page matches its golden file, and calls only the scripts we expect.</summary>
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

    /// <summary>A message opens a session and streams the turn, and the page matches its golden file.</summary>
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
        Snapshots.Match("Pages/ChatPage.turn.html", cut.Markup);
    }
}
