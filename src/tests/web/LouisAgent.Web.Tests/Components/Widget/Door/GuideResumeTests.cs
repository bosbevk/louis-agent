using System.Text.Json;
using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;
using ChatModel = LouisAgent.Web.Chats.Chat;

namespace LouisAgent.Web.Tests.Components.Widget.Door;

/// <summary>
/// The guide's talk outlives the page (F151.6, Louis: "guide must never lose context across pages", "or navigation",
/// and his pick "A reload picks the talk up"): a widget that starts with a talk saved in the browser shows it and
/// carries on in its session, and when the server has let that session go, the question goes again in a new talk.
/// </summary>
public class GuideResumeTests
{
    /// <summary>Where the widget keeps its talks in the browser.</summary>
    private const string ChatsKey = "louis-agent.widget-chats";

    /// <summary>A session the stub API has never heard of, as after a restart: a message to it is a 404.</summary>
    private const string GoneSession = "sess_gone";

    /// <summary>
    /// A context whose browser holds one saved talk, held in the session named, over the stub API.
    /// </summary>
    private static BunitContext Context(string session, out StubHandler handler)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.JSInterop.Setup<string?>("localStorage.getItem", ChatsKey).SetResult(SavedTalk(session));

        var settings = new ChatSettings();
        handler = new StubHandler(FixtureApi.Answer);
        var api = new AgentApi(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") }, settings);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>One talk with one answer, as the store writes it to the browser.</summary>
    private static string SavedTalk(string session)
    {
        var answered = new Turn
        {
            UserText = "What do you build?",
            Blocks = [new Block { Kind = BlockKind.Text, Text = "Websites with an agent." }]
        };
        var talk = new ChatModel
        {
            SessionId = session,
            Turns = [answered]
        };
        List<ChatModel> saved = [talk];
        return JsonSerializer.Serialize(saved, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    /// <summary>The bar, as a site's layout places it.</summary>
    private static IRenderedComponent<ChatWidget> Bar(BunitContext ctx) =>
        ctx.Render<ChatWidget>(parameters => parameters
            .Add(widget => widget.Identity, "guide")
            .Add(widget => widget.Title, "Louis · AI")
            .Add(widget => widget.Layout, WidgetLayout.Bar));

    /// <summary>The messages the API was sent, by the session each went to.</summary>
    private static List<string> MessagesSent(StubHandler handler) =>
        handler.Requests.Where(request => request.Url.EndsWith("/messages"))
            .Select(request => request.Url.Split('/')[^2])
            .ToList();

    /// <summary>After a reload the saved talk stands over the bar again, and the next question goes to its session.</summary>
    [Test]
    public void ASavedTalk_ComesBack_AndCarriesOnInItsSession()
    {
        using var ctx = Context(FixtureApi.SessionId, out var handler);
        var cut = Bar(ctx);
        cut.WaitForElement(".caption-says", TimeSpan.FromSeconds(10));
        string shown = cut.Find(".caption-says").TextContent;

        cut.Find("#composer-input").Input("And what does it cost?");
        cut.Find("button.send").Click();
        cut.WaitForState(() => MessagesSent(handler).Count > 0, TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(shown, Does.Contain("Websites with an agent."));
            Assert.That(MessagesSent(handler), Is.EqualTo(new[] { FixtureApi.SessionId }));
            Assert.That(handler.Requests.Where(request => request.Url.EndsWith("/v1/sessions")), Is.Empty,
                "no new session: the talk carries on in its own");
        });
    }

    /// <summary>
    /// The server no longer knows the saved talk's session: the visitor's question is asked again in a new talk, so
    /// they get their answer and never a bar that takes no more messages.
    /// </summary>
    [Test]
    public void AGoneSession_AsksTheQuestionAgain_InANewTalk()
    {
        using var ctx = Context(GoneSession, out var handler);
        var cut = Bar(ctx);
        cut.WaitForElement(".caption-says", TimeSpan.FromSeconds(10));

        cut.Find("#composer-input").Input("And what does it cost?");
        cut.Find("button.send").Click();
        cut.WaitForState(() => MessagesSent(handler).Count > 1, TimeSpan.FromSeconds(10));
        var asked = handler.Requests.Where(request => request.Url.EndsWith("/messages")).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(MessagesSent(handler), Is.EqualTo(new[] { GoneSession, FixtureApi.SessionId }));
            Assert.That(asked[1].Body, Does.Contain("And what does it cost?"));
            Assert.That(cut.Find("#composer-input").HasAttribute("disabled"), Is.False);
        });
    }
}
