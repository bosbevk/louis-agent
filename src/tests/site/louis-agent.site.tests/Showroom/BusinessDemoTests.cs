using System.Net;
using System.Text;
using LouisAgent.Site.Components.Showroom;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Components.Recorded;
using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Stage;
using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Showroom;

/// <summary>
/// F127.2 (Louis: "when the demo finishes there should be a button try it yourself"): once a business's recording has
/// played, the visitor can try its agent live in the same corner of its website, and go back to the recording.
/// </summary>
public class BusinessDemoTests
{
    /// <summary>One recorded turn: the agent shows the rooms, then answers.</summary>
    private const string Recording = """
                                     event: message_start
                                     data: {"type":"message_start","message":{"session_id":"sess_rec","role":"assistant"}}

                                     event: content_block_start
                                     data: {"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"toolu_show","name":"ShowPage","input":{"path":"/rooms"}}}

                                     event: content_block_stop
                                     data: {"type":"content_block_stop","index":0}

                                     event: content_block_start
                                     data: {"type":"content_block_start","index":1,"content_block":{"type":"text","text":""}}

                                     event: content_block_delta
                                     data: {"type":"content_block_delta","index":1,"delta":{"type":"text_delta","text":"Hi."}}

                                     event: content_block_stop
                                     data: {"type":"content_block_stop","index":1}

                                     event: message_delta
                                     data: {"type":"message_delta","delta":{"stop_reason":"end_turn"}}

                                     event: message_stop
                                     data: {"type":"message_stop"}

                                     """;

    /// <summary>
    /// A context with what the demo asks for: the recording served as the site serves it, and the site's API for a live
    /// try, stubbed.
    /// </summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(Recording, Encoding.UTF8, "text/event-stream")
        }))
        { BaseAddress = new Uri("http://site.test/") });
        var settings = new ChatSettings();
        var api = new AgentApi(
            new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound)))
            {
                BaseAddress = new Uri("http://site.test/")
            }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        return ctx;
    }

    /// <summary>The guesthouse's demo, its conversation one turn long.</summary>
    private static IRenderedComponent<BusinessDemo> Demo(BunitContext ctx) =>
        ctx.Render<BusinessDemo>(parameters => parameters
            .Add(demo => demo.Accent, TileAccent.Yellow)
            .Add(demo => demo.Kicker, "A guesthouse")
            .Add(demo => demo.Business, "Seemeeu Guesthouse")
            .Add(demo => demo.Heading, "A weekend by the beach, with the dog.")
            .Add(demo => demo.Lede, "A couple asks about a weekend away.")
            .Add(demo => demo.Address, "seemeeu.example")
            .Add(demo => demo.Colours, new MockColours("#0b3a4a", "#e9dcc3", "#fbf8f2", "#14262d"))
            .Add(demo => demo.Mark, (RenderFragment)(_ => { }))
            .Add(demo => demo.Tagline, "Guesthouse · Yzerfontein")
            .Add(demo => demo.HeroLine, "Four rooms, five minutes from the beach.")
            .Add(demo => demo.HeroPhoto, "images/showroom/seemeeu-banner.webp")
            .Add(demo => demo.HeroPhotoAlt, "Waves on the rocks")
            .Add(demo => demo.HeroAction, "Book a room")
            .Add(demo => demo.Pages, (RenderFragment)(_ => { }))
            .Add(demo => demo.KindOf, "guesthouse")
            .Add(demo => demo.Turns, [new RecordedTurn("Do you take dogs?", "recordings/showroom/test-1.sse")])
            .Add(demo => demo.Cues, Array.Empty<DemoCue>())
            .Add(demo => demo.Identity, "seemeeu-guesthouse")
            .Add(demo => demo.Hint, "Is breakfast included?"));

    /// <summary>The demo once its recording has played to the end.</summary>
    private static IRenderedComponent<BusinessDemo> Played(BunitContext ctx)
    {
        var cut = Demo(ctx);
        cut.WaitForElement(".recorded-end", TimeSpan.FromSeconds(10));
        return cut;
    }

    /// <summary>
    /// Presses Try it yourself until the live chat is there. The ended recording can redraw once more just after the
    /// button is found, so a press may land on the drawing before; pressing again does no harm, since Try only goes
    /// live.
    /// </summary>
    private static void TryIt(IRenderedComponent<BusinessDemo> cut) =>
        cut.WaitForAssertion(() =>
        {
            if (cut.FindAll(".demo-try") is [var button, ..]) button.Click();
            Assert.That(cut.FindAll(".live-chat"), Is.Not.Empty);
        }, TimeSpan.FromSeconds(5));

    /// <summary>The guide's ask to a business, as the guide's widget hands it on.</summary>
    private static Block GuidesAsk(string address) => new()
    {
        Kind = BlockKind.Tool,
        ToolName = Delegation.AskToolName,
        ToolInput = $$"""{"address":"{{address}}","message":"A room for Friday, with our dog?"}"""
    };

    /// <summary>
    /// The guide runs the demo live (F127.4.2): when it asks this business, even while the recording plays, the live
    /// chat takes the recording's place and shows that talk; an ask to another business leaves the demo as it is.
    /// </summary>
    [Test]
    public void TheGuideAskingThisBusiness_GoesLive_WithThatTalk()
    {
        using var ctx = Context();
        var windows = new AgentWindows();
        ctx.Services.AddSingleton(windows);
        var cut = Demo(ctx);

        cut.InvokeAsync(() => windows.Take(GuidesAsk("koringland-attorneys")));
        bool otherWentLive = cut.FindAll(".live-chat").Count > 0;
        cut.InvokeAsync(() => windows.Take(GuidesAsk("seemeeu-guesthouse")));

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".live-chat"), Is.Not.Empty), TimeSpan.FromSeconds(5));
        Assert.Multiple(() =>
        {
            Assert.That(otherWentLive, Is.False);
            Assert.That(windows.Shows("seemeeu-guesthouse"), Is.True);
            Assert.That(cut.FindAll(".recorded-chat"), Is.Empty);
        });
    }

    /// <summary>Try it yourself waits for the play to end, then sits beside Play it again.</summary>
    [Test]
    public void TryItYourself_ShowsOnceThePlayHasEnded()
    {
        using var ctx = Context();
        var cut = Demo(ctx);
        var whilePlaying = cut.FindAll(".demo-try").Count;

        cut.WaitForElement(".recorded-end", TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(whilePlaying, Is.Zero);
            Assert.That(cut.Find(".recorded-end .demo-try").TextContent.Trim(), Is.EqualTo("Try it yourself"));
        });
    }

    /// <summary>
    /// Trying it swaps the recording for a live chat with the business's agent, says it's live, and sends the website
    /// home, so the visitor starts where a new customer would.
    /// </summary>
    [Test]
    public void TryingIt_SwapsTheChat_AndSendsTheWebsiteHome()
    {
        using var ctx = Context();
        var cut = Played(ctx);
        var before = cut.Find(".mock-address").TextContent;

        TryIt(cut);

        Assert.Multiple(() =>
        {
            Assert.That(before, Is.EqualTo("seemeeu.example/rooms"));
            Assert.That(cut.FindAll(".recorded-chat"), Is.Empty);
            Assert.That(cut.Find(".live-hint-words").TextContent, Is.EqualTo("Is breakfast included?"));
            Assert.That(cut.Find("#composer-input").GetAttribute("placeholder"),
                Is.EqualTo("Ask Seemeeu Guesthouse…"));
            Assert.That(cut.FindAll(".mock-live-tag"), Has.Count.EqualTo(1));
            Assert.That(cut.Find(".mock-address").TextContent, Is.EqualTo("seemeeu.example"));
        });
    }

    /// <summary>
    /// While the visitor tries it live, the cues wait (Louis's pick of three, after a cue clicked live crashed the
    /// page), and they come back with the recording.
    /// </summary>
    [Test]
    public void TheCues_WaitWhileLive()
    {
        using var ctx = Context();
        var cut = Played(ctx);

        TryIt(cut);
        var live = cut.Find(".demo-cues").ClassList.Contains("is-waiting");
        cut.Find(".live-leave").Click();

        Assert.Multiple(() =>
        {
            Assert.That(live, Is.True);
            Assert.That(cut.Find(".demo-cues").ClassList, Does.Not.Contain("is-waiting"));
        });
    }

    /// <summary>Watch the recording takes the visitor back to the recorded conversation.</summary>
    [Test]
    public void WatchingTheRecording_SwapsBack()
    {
        using var ctx = Context();
        var cut = Played(ctx);
        TryIt(cut);

        cut.Find(".live-leave").Click();

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".live-chat"), Is.Empty);
            Assert.That(cut.FindAll(".recorded-chat"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".mock-live-tag"), Is.Empty);
        });
    }
}
