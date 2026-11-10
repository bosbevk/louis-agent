using System.Net;
using System.Text;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Site.Showroom.Mock;
using LouisAgent.Web.Components.Site.Showroom.Mock.Live;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Site.Showroom.Mock.Live;

/// <summary>
/// F127.2 (Louis: "Try it yourself"): a business's agent, live, in the corner of its mocked website. A question opens a
/// session as the business, the mock's own page goes with it, and a place the agent shows is handed on, as a recorded
/// one is.
/// </summary>
public class MockLiveChatTests
{
    private const string Hint = "Try: a room for Friday, with our dog?";

    /// <summary>A turn in which the guesthouse's agent shows the garden room, and nothing else.</summary>
    private const string ShowPageStream = """
                                          event: message_start
                                          data: {"type":"message_start","message":{"session_id":"sess_fixture","role":"assistant"}}

                                          event: content_block_start
                                          data: {"type":"content_block_start","index":0,"content_block":{"type":"tool_use","id":"toolu_show","name":"ShowPage","input":{"path":"/rooms/garden"}}}

                                          event: content_block_stop
                                          data: {"type":"content_block_stop","index":0}

                                          event: message_delta
                                          data: {"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"input_tokens":1,"output_tokens":1,"requests":1}}

                                          event: message_stop
                                          data: {"type":"message_stop"}

                                          """;

    /// <summary>
    /// A context with what the chat asks for, over the stub API, which remembers what was sent; with a stream, a
    /// message is answered with it instead of the recorded turn.
    /// </summary>
    private static BunitContext Context(out StubHandler handler, string? stream = null)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        handler = new StubHandler(request =>
            stream is not null && request.RequestUri!.AbsolutePath.EndsWith("/messages")
                ? new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(stream, Encoding.UTF8, "text/event-stream")
                }
                : FixtureApi.Answer(request));
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>The guesthouse's mock with its agent live, what it shows kept in <paramref name="shown"/>.</summary>
    private static IRenderedComponent<SiteMock> Mock(BunitContext ctx, List<Block>? shown = null)
    {
        RenderFragment live = builder =>
        {
            builder.OpenComponent<MockLiveChat>(0);
            builder.AddComponentParameter(1, nameof(MockLiveChat.Identity), "seemeeu-guesthouse");
            builder.AddComponentParameter(2, nameof(MockLiveChat.Hint), Hint);
            builder.AddComponentParameter(3, nameof(MockLiveChat.ToolCalled),
                EventCallback.Factory.Create<Block>(new object(), block => shown?.Add(block)));
            builder.CloseComponent();
        };
        return ctx.Render<SiteMock>(parameters => parameters
            .Add(mock => mock.Address, "seemeeu.example")
            .Add(mock => mock.Business, "Seemeeu")
            .Add(mock => mock.Colours, new MockColours("#0b3a4a", "#e9dcc3", "#fbf8f2", "#14262d"))
            .Add(mock => mock.HeroLine, "Four rooms by the beach.")
            .Add(mock => mock.Agent, live));
    }

    /// <summary>Before the first question the chat offers one to ask.</summary>
    [Test]
    public void BeforeTheFirstQuestion_TheHintShows()
    {
        using var ctx = Context(out _);

        var cut = Mock(ctx);

        Assert.That(cut.Find(".mock-widget-body .live-hint").TextContent, Is.EqualTo(Hint));
    }

    /// <summary>A question opens a session as the business, with no toolsets of its own, and the turn shows.</summary>
    [Test]
    public void AQuestion_OpensASession_AsTheBusiness()
    {
        using var ctx = Context(out var handler);
        var cut = Mock(ctx);

        Send(cut, "A room for Friday, with our dog?");

        var opened = handler.Requests.Single(r => r.Method == HttpMethod.Post && r.Url.EndsWith("/v1/sessions"));
        Assert.That(opened.Body,
            Does.Contain("\"identity\":\"seemeeu-guesthouse\"").And.Contain("\"toolsets\":[]"));
        Assert.That(cut.FindAll(".turn"), Has.Count.EqualTo(1));
    }

    /// <summary>
    /// The agent reads the business's website, not the site it sits in: the mock's page goes with the first question,
    /// not again from the same page, and again once the website is on another.
    /// </summary>
    [Test]
    public void TheMocksPage_GoesWithTheFirstQuestion_AndAgainOnAnotherPage()
    {
        using var ctx = Context(out var handler);
        var module = ctx.JSInterop.SetupModule(FrameworkScripts.Mock);
        module.Setup<Page?>("pageOf", _ => true).SetResult(new Page("/", "Seemeeu", "Four rooms by the beach.",
            [new PageArea("/rooms", "Rooms (page)"), new PageArea("/rooms/garden", "The garden room")]));
        var cut = Mock(ctx);

        Send(cut, "Do you take dogs?");
        Send(cut, "And breakfast?");
        module.Setup<Page?>("pageOf", _ => true).SetResult(new Page("/rooms", "Seemeeu", "Rooms.", []));
        Send(cut, "Which room is that?");

        var bodies = handler.Requests.Where(r => r.Url.EndsWith("/messages")).Select(r => r.Body!).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(bodies, Has.Count.EqualTo(3));
            Assert.That(bodies[0], Does.Contain("\"url\":\"/\"").And.Contain("The garden room"));
            Assert.That(bodies[1], Does.Not.Contain("The garden room"), "the same page goes once");
            Assert.That(bodies[2], Does.Contain("\"url\":\"/rooms\""));
        });
    }

    /// <summary>A place the agent shows is handed on as the turn streams, so the demo moves the website.</summary>
    [Test]
    public void AShowPageCall_IsHandedOn()
    {
        using var ctx = Context(out _, ShowPageStream);
        var shown = new List<Block>();
        var cut = Mock(ctx, shown);

        Send(cut, "Do you take dogs?");

        Assert.That(shown.Select(ShowPageCall.PathOf), Is.EqualTo(new[] { "/rooms/garden" }));
    }

    /// <summary>Types a message into the chat's composer, sends it and waits for the turn to show and end.</summary>
    private static void Send(IRenderedComponent<SiteMock> cut, string text)
    {
        var turns = cut.FindAll(".turn").Count;
        cut.Find("#composer-input").Input(text);
        cut.Find("button.send").Click();
        cut.WaitForState(() => cut.FindAll(".turn").Count > turns && cut.FindAll("button.send").Count == 1,
            TimeSpan.FromSeconds(10));
    }
}
