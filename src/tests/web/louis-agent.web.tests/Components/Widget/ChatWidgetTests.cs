using System.Net;
using System.Text;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Widget.Door;
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

    /// <summary>A context whose API answers a message with the given stream instead of the recorded turn.</summary>
    private static BunitContext ContextStreaming(string sse)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var handler = new StubHandler(request => request.RequestUri!.AbsolutePath.EndsWith("/messages")
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(sse, Encoding.UTF8, "text/event-stream")
            }
            : FixtureApi.Answer(request));
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(new SiteAddresses("home", ["/", "/identities"]));
        return ctx;
    }

    /// <summary>A turn in which the model calls ShowPage for the levels on Identities, and nothing else.</summary>
    private const string ShowPageStream = """
        event: message_start
        data: {"type":"message_start","message":{"session_id":"sess_fixture","role":"assistant"}}

        event: content_block_start
        data: {"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"toolu_show","name":"ShowPage","input":{"path":"/identities/levels"}}}

        event: content_block_stop
        data: {"type":"content_block_stop","index":0}

        event: content_block_start
        data: {"type":"content_block_start","index":1,"content_block":{"type":"tool_result","tool_use_id":"toolu_show","content":"Shown: the visitor's browser is now on /identities#levels.","is_error":false}}

        event: content_block_stop
        data: {"type":"content_block_stop","index":1}

        event: message_delta
        data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":1,"output_tokens":1,"requests":1}}

        event: message_stop
        data: {"type":"message_stop"}

        """;

    /// <summary>
    /// The guide takes the visitor to a page (F117, Louis: "I want this agent to take you to a page", "direct you to a
    /// certain part of the page"): on a ShowPage call in the turn the widget moves the browser to the path and asks its
    /// module to show the area, scrolled to and lit up.
    /// </summary>
    [Test]
    public void AShowPageCall_MovesTheBrowser_AndShowsTheArea()
    {
        using var ctx = ContextStreaming(ShowPageStream);
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Widget);
        module.Setup<Page>("pageText").SetResult(new Page("/", "louis-agent", "An AI agent framework.", []));
        module.SetupVoid("showArea", _ => true);
        var cut = Render(ctx);
        cut.Find("button.widget-button").Click();

        Send(cut, "where are the levels?");

        var shown = module.Invocations.Where(call => call.Identifier == "showArea").ToList();
        Assert.Multiple(() =>
        {
            Assert.That(ctx.Services.GetRequiredService<NavigationManager>().Uri, Does.EndWith("/identities"));
            Assert.That(shown.Select(call => call.Arguments[0]), Is.EqualTo(new[] { "levels" }));
            Assert.That(shown.Select(call => call.Arguments[2]), Is.EqualTo(new[] { "/identities/levels" }),
                "the bar shows the place's own address (ADR 0107)");
        });
    }

    /// <summary>
    /// A button anywhere asks the guide through the door (F118, "take the tour"): the panel opens and the question goes
    /// as if the visitor had typed it.
    /// </summary>
    [Test]
    public void AQuestionAtTheDoor_OpensThePanel_AndGoes()
    {
        using var ctx = Context(out var handler);
        var door = new GuideDoor();
        ctx.Services.AddSingleton(door);
        var cut = Render(ctx);

        cut.InvokeAsync(() => door.Ask("Walk me through this site."));

        cut.WaitForState(() => cut.FindAll(".turn").Count > 0, TimeSpan.FromSeconds(10));
        var sent = handler.Requests.Where(r => r.Url.EndsWith("/messages")).Select(r => r.Body!).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(sent, Has.Count.EqualTo(1));
            Assert.That(sent[0], Does.Contain("Walk me through this site."));
            Assert.That(cut.FindAll(".widget-panel"), Is.Not.Empty, "the panel is open");
        });
    }

    /// <summary>
    /// Every page offers its own tour (F118, Louis: "each page should have the take a tour option for the page"): with
    /// a label set, the folded widget shows the tour button above its own, and a click opens the guide and asks it to
    /// walk the visitor through the page; without one, there is none.
    /// </summary>
    [Test]
    public void ThePagesTour_SitsAboveTheButton_AndAsksForAWalkthrough()
    {
        using var ctx = Context(out var handler);
        ctx.Services.AddSingleton(new GuideDoor());
        var cut = ctx.Render<ChatWidget>(parameters => parameters
            .Add(w => w.Identity, "guide")
            .Add(w => w.TourLabel, "Tour this page"));
        var without = Render(ctx);

        var tour = cut.Find(".chat-widget > button.guide-button.widget-tour");
        string label = tour.TextContent;
        tour.Click();

        cut.WaitForState(() => cut.FindAll(".turn").Count > 0, TimeSpan.FromSeconds(10));
        var sent = handler.Requests.Where(r => r.Url.EndsWith("/messages")).Select(r => r.Body!).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(label, Does.Contain("Tour this page"));
            Assert.That(sent, Has.Some.Contains("Walk me through this page"));
            Assert.That(cut.FindAll(".widget-tour"), Is.Empty, "the tour hides while the chat is open");
            Assert.That(without.FindAll(".widget-tour"), Is.Empty);
        });
    }

    /// <summary>
    /// With a tour, the folded corner offers the tour alone (FX114, Louis: "we can take ask the guide out, just have
    /// tour this page"): the tour opens the guide, and the guide's own button is back only as its close.
    /// </summary>
    [Test]
    public void WithATour_TheFoldedCornerIsTheTourAlone()
    {
        using var ctx = Context(out _);
        ctx.Services.AddSingleton(new GuideDoor());
        var cut = ctx.Render<ChatWidget>(parameters => parameters
            .Add(w => w.Identity, "guide")
            .Add(w => w.TourLabel, "Tour this page"));

        var folded = cut.FindAll(".chat-widget > button").Select(b => b.ClassName).ToList();
        cut.Find(".widget-tour").Click();

        Assert.Multiple(() =>
        {
            Assert.That(folded, Has.Count.EqualTo(1));
            Assert.That(folded[0], Does.Contain("widget-tour"));
            Assert.That(cut.FindAll(".widget-button"), Has.Count.EqualTo(1), "open, the guide can be closed");
        });
    }

    /// <summary>
    /// A visitor who closes the guide mid-tour can pick it up again (FX117, Louis: "when the user clicks out he should
    /// be able to resume tour", "he must continue from where they left off or from the new page"): once a
    /// conversation has started, the folded corner offers the resume label, and it opens the guide on the same
    /// conversation and tells it to carry on, with the page the visitor is on now.
    /// </summary>
    [Test]
    public void AfterATourStarted_TheCornerResumesIt()
    {
        using var ctx = Context(out var handler);
        ctx.Services.AddSingleton(new GuideDoor());
        var cut = ctx.Render<ChatWidget>(parameters => parameters
            .Add(w => w.Identity, "guide")
            .Add(w => w.TourLabel, "Tour this page")
            .Add(w => w.ResumeLabel, "Resume the tour"));
        cut.Find(".widget-tour").Click();
        // The first answer has streamed to its end, as a visitor would wait, before the guide is closed.
        cut.WaitForState(() => cut.FindAll(".turn .markdown").Count > 0 && cut.FindAll("button.stop").Count == 0,
            TimeSpan.FromSeconds(10));

        cut.Find(".widget-button").Click();
        string folded = cut.WaitForElement(".widget-tour", TimeSpan.FromSeconds(10)).TextContent;
        cut.Find(".widget-tour").Click();
        cut.WaitForState(() => handler.Requests.Count(r => r.Url.EndsWith("/messages")) == 2, TimeSpan.FromSeconds(10));

        var sent = handler.Requests.Where(r => r.Url.EndsWith("/messages")).Select(r => r.Body!).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(folded, Does.Contain("Resume the tour"));
            Assert.That(cut.FindAll(".widget-panel"), Is.Not.Empty, "the guide is open again");
            Assert.That(handler.Requests.Count(r => r.Method == HttpMethod.Post && r.Url.EndsWith("/v1/sessions")),
                Is.EqualTo(1), "on the same conversation");
            Assert.That(sent[1], Does.Contain("carry on with the tour"));
        });
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

    /// <summary>
    /// A question opens a session as the widget's identity, with no toolsets of its own, and the turn shows.
    /// </summary>
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
        module.Setup<Page>("pageText")
            .SetResult(new Page("/build-your-own", "Build your own", "A few lines of your own.", []));
        var cut = Render(ctx);
        cut.Find("button.widget-button").Click();

        module.Setup<PageArea?>("inView").SetResult(new PageArea("program", "The whole program"));
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
            Assert.That(bodies[1], Does.Not.Contain("A few lines of your own."), "the same page's words go once");
            Assert.That(bodies[1], Does.Contain("\"inView\":{\"id\":\"program\",\"heading\":\"The whole program\"}"),
                "where they are on it goes every time (F118)");
            Assert.That(bodies[2], Does.Contain("\"url\":\"/packs\""));
        });
    }

    /// <summary>
    /// A card open on the visitor's screen goes with every message (FX106, Louis's screenshot: on the leads pack's
    /// card, "this pack" was taken for the one the guide had opened before), so "this" is the card they opened.
    /// </summary>
    [Test]
    public void AnOpenCard_TravelsWithEveryMessage()
    {
        using var ctx = Context(out var handler);
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Widget);
        module.Setup<Page>("pageText").SetResult(new Page("/packs", "Packs", "A pack is tools and their guide.", []));
        module.Setup<PageArea?>("inView").SetResult(new PageArea("general", "General tools"));
        module.Setup<PageArea?>("openCard").SetResult(new PageArea("louis-agent-pack-leads", "louis-agent.pack.leads"));
        var cut = Render(ctx);
        cut.Find("button.widget-button").Click();

        Send(cut, "tell me about this pack");
        Send(cut, "and this one?");

        var bodies = handler.Requests.Where(r => r.Url.EndsWith("/messages")).Select(r => r.Body!).ToList();
        Assert.That(bodies, Has.All.Contain("\"open\":{\"id\":\"louis-agent-pack-leads\""));
    }

    /// <summary>
    /// Types a message into the widget's composer, sends it and waits for the turn to show and the stream to end.
    /// </summary>
    private static void Send(IRenderedComponent<ChatWidget> cut, string text)
    {
        int turns = cut.FindAll(".turn").Count;
        cut.Find("#composer-input").Input(text);
        cut.Find("button.send").Click();
        cut.WaitForState(() => cut.FindAll(".turn").Count > turns && cut.FindAll("button.send").Count == 1,
            TimeSpan.FromSeconds(10));
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

        Assert.That(handler.Requests.Where(r => r.Method == HttpMethod.Get && r.Url.EndsWith("/v1/rates")),
            Is.Not.Empty);
    }
}
