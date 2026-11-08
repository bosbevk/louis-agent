using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget;

/// <summary>
/// The chat widget (F109.3): a button folded in a corner, a panel with the chat's own list and composer on a runner
/// of its own, opened as the identity it is given. A page drops it in with one tag; the words are parameters, so the
/// framework names no site.
/// </summary>
public class ChatWidgetTests
{
    /// <summary>A context with what the widget asks for, over the stub API, which remembers what was sent.</summary>
    private static BunitContext Context(out StubHandler handler)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out handler);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    private static IRenderedComponent<ChatWidget> Render(BunitContext ctx) =>
        ctx.Render<ChatWidget>(parameters => parameters
            .Add(w => w.Identity, "guide")
            .Add(w => w.Title, "Ask the guide")
            .Add(w => w.Greeting, "Ask me anything about this framework.")
            .Add(w => w.Placeholder, "Your question…"));

    /// <summary>Folded, it is one button that says what it opens, and nothing else is drawn.</summary>
    [Test]
    public void Folded_ItIsOneButton()
    {
        using var ctx = Context(out _);
        var cut = Render(ctx);

        Assert.That(cut.FindAll(".widget-panel"), Is.Empty);
        Assert.That(cut.Find("button.widget-button").GetAttribute("aria-expanded"), Is.EqualTo("false"));
        Snapshots.Match("Components/Widget/ChatWidget.folded.html", cut.Markup);
    }

    /// <summary>Opened, it shows its title, its greeting and the composer; closed again, only the button.</summary>
    [Test]
    public void Opened_ItShowsTheGreetingAndTheComposer_AndFoldsAgain()
    {
        using var ctx = Context(out _);
        var cut = Render(ctx);

        cut.Find("button.widget-button").Click();
        Assert.That(cut.Find(".widget-title").TextContent, Is.EqualTo("Ask the guide"));
        Assert.That(cut.Find(".widget-greeting").TextContent, Does.Contain("Ask me anything"));
        Assert.That(cut.FindAll("#composer-input"), Has.Count.EqualTo(1));
        Assert.That(cut.Find("button.widget-button").GetAttribute("aria-expanded"), Is.EqualTo("true"));
        Snapshots.Match("Components/Widget/ChatWidget.open.html", cut.Markup);

        cut.Find("button.widget-close").Click();
        Assert.That(cut.FindAll(".widget-panel"), Is.Empty);
    }

    /// <summary>
    /// The composer's buttons are the chat's glyphs, not the words, which a 40-pixel button clips (FX69): Send is the
    /// arrow and the word is its title.
    /// </summary>
    [Test]
    public void TheSendButton_IsTheChatsGlyph_NotAWord()
    {
        using var ctx = Context(out _);
        var cut = Render(ctx);
        cut.Find("button.widget-button").Click();

        var send = cut.Find("button.send");

        Assert.That(send.TextContent.Trim(), Is.EqualTo("↑"));
        Assert.That(send.GetAttribute("title"), Is.Not.Empty);
    }

    /// <summary>
    /// Enter sends in the widget too (FX70): the composer attaches its own key rule to its box when it draws, so the
    /// rule reaches a box outside the chat shell.
    /// </summary>
    [Test]
    public void TheComposer_AttachesItsKeyRule_WhenItDraws()
    {
        using var ctx = Context(out _);
        var cut = Render(ctx);

        cut.Find("button.widget-button").Click();

        cut.WaitForAssertion(() =>
            Assert.That(ctx.JSInterop.Invocations.Select(call => call.Identifier), Does.Contain("attach")));
        Assert.That(ctx.JSInterop.Invocations.Where(call => call.Identifier == "import")
            .Select(call => call.Arguments[0]), Does.Contain(FrameworkScripts.Composer));
    }

    /// <summary>A question opens a session as the widget's identity, with no toolsets of its own, and the turn shows.</summary>
    [Test]
    public void AQuestion_OpensASession_AsTheIdentity()
    {
        using var ctx = Context(out var handler);
        var cut = Render(ctx);
        cut.Find("button.widget-button").Click();

        cut.Find("#composer-input").Input("How does a pack stay convertible?");
        cut.Find("button.send").Click();
        cut.WaitForState(() => cut.FindAll(".turn").Count > 0, TimeSpan.FromSeconds(10));

        var opened = handler.Requests.Single(r => r.Method == HttpMethod.Post && r.Url.EndsWith("/v1/sessions"));
        Assert.That(opened.Body, Does.Contain("\"identity\":\"guide\"").And.Contain("\"toolsets\":[]"));
        Assert.That(cut.FindAll(".turn"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// The guide knows the page (F115, Louis: "the agent should know which page im on"): the widget reads the page's
    /// address, title and text through its module when it opens and sends them with the first message; the next
    /// message on the same page carries no page; after a move to another page the next message carries that one.
    /// </summary>
    [Test]
    public void ThePage_TravelsWithTheFirstMessage_AndAgainAfterAMove()
    {
        using var ctx = Context(out var handler);
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Widget);
        module.Setup<Page>("pageText").SetResult(new Page("/build-your-own", "Build your own", "A few lines of your own.", []));
        var cut = Render(ctx);
        cut.Find("button.widget-button").Click();

        Send(cut, "which page am I on?");
        Send(cut, "and then?");
        module.Setup<Page>("pageText").SetResult(new Page("/packs", "Packs", "A pack is tools and their guide.", []));
        ctx.Services.GetRequiredService<NavigationManager>().NavigateTo("/packs");
        Send(cut, "and here?");

        var bodies = handler.Requests.Where(r => r.Url.EndsWith("/messages")).Select(r => r.Body!).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(bodies, Has.Count.EqualTo(3));
            Assert.That(bodies[0], Does.Contain("\"url\":\"/build-your-own\"").And.Contain("A few lines of your own."));
            Assert.That(bodies[1], Does.Not.Contain("\"page\""), "the same page again carries nothing");
            Assert.That(bodies[2], Does.Contain("\"url\":\"/packs\""));
        });
    }

    /// <summary>Types a message into the widget's composer, sends it and waits for the turn to show and the stream to end.</summary>
    private static void Send(IRenderedComponent<ChatWidget> cut, string text)
    {
        int turns = cut.FindAll(".turn").Count;
        cut.Find("#composer-input").Input(text);
        cut.Find("button.send").Click();
        cut.WaitForState(() => cut.FindAll(".turn").Count > turns && cut.FindAll("button.send").Count == 1, TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// The widget asks the API for the rates when it draws (FX80, Louis: "cost in rand"), so a turn's cost shows in the
    /// site's currency and not as charged; the chat page does this through its switcher, the widget has none.
    /// </summary>
    [Test]
    public void TheWidget_AsksForTheRates_WhenItDraws()
    {
        using var ctx = Context(out var handler);

        ctx.Render<ChatWidget>(parameters => parameters.Add(widget => widget.Toolsets, new[] { "docs" }));

        Assert.That(handler.Requests.Where(r => r.Method == HttpMethod.Get && r.Url.EndsWith("/v1/rates")), Is.Not.Empty);
    }
}
