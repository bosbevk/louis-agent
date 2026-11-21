using LouisAgent.Choices;
using LouisAgent.Identities.Guide;
using LouisAgent.Site.Components.Guide;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Site.Tests.Components.Guide;

/// <summary>
/// The site's guide (F109.3): the framework's widget wrapped once with the expert identity and the site's words,
/// as the bar at the page's foot since F151.6, so a layout places one tag and a question opens a session as the
/// expert.
/// </summary>
public class ExpertChatTests
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

    /// <summary>
    /// The guide is the bar at the page's foot (F151.6, Louis: "these button must go we will use the same guide as
    /// in design chat widget everywhere"): its box is always there, and no tour button stands in a corner.
    /// </summary>
    [Test]
    public void TheGuide_IsTheBar_WithNoTourButton()
    {
        using var ctx = Context(out _);
        ctx.Services.AddSingleton(new GuideDoor());
        var cut = ctx.Render<ExpertChat>();

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".chat-widget").ClassList, Does.Contain("is-bar"));
            Assert.That(cut.Find(".agent-doing").TextContent, Is.EqualTo("Orbi · AI"));
            Assert.That(cut.FindAll("button.widget-tour"), Is.Empty);
            Assert.That(cut.FindAll("button.widget-button"), Is.Empty);
        });
    }

    /// <summary>A question typed in the bar's box opens a session as the expert.</summary>
    [Test]
    public void AQuestionInTheBar_OpensAsTheExpert()
    {
        using var ctx = Context(out var handler);
        ctx.Services.AddSingleton(new GuideDoor());
        var cut = ctx.Render<ExpertChat>();

        cut.Find("#composer-input").Input("What is this site?");
        cut.Find("button.send").Click();
        cut.WaitForState(
            () => handler.Requests.Any(r => r.Method == HttpMethod.Post && r.Url.EndsWith("/v1/sessions")),
            TimeSpan.FromSeconds(10));
        var opened = handler.Requests.Single(r => r.Method == HttpMethod.Post && r.Url.EndsWith("/v1/sessions"));

        Assert.That(opened.Body, Does.Contain($"\"identity\":\"{GuideIdentityName.LouisAgentGuide.Wire()}\""));
    }

    /// <summary>
    /// The guide is Orbi, Louis's AI agent (Louis, 2026-11-18: "Orbi it is"), labelled AI so nobody takes it for a
    /// person: it says so in its first words, and asks what brings the visitor in a plain way.
    /// </summary>
    [Test]
    public void TheGuide_IsOrbi_SaysItIsAI_AndAsksWhatBringsYou()
    {
        using var ctx = Context(out _);
        ctx.Services.AddSingleton(new GuideDoor());

        var widget = ctx.Render<ExpertChat>().FindComponent<ChatWidget>().Instance;

        Assert.Multiple(() =>
        {
            Assert.That(widget.Title, Is.EqualTo("Orbi · AI"));
            Assert.That(widget.Greeting, Does.StartWith("Hi, I'm Orbi, Louis's AI agent."));
            Assert.That(widget.Greeting, Does.Contain("What brings you here?"));
            Assert.That(widget.Greeting, Does.Not.Contain(";").And.Not.Contain(":"));
            Assert.That(widget.ShowWork, Is.False, "a visitor sees its words, not its tool calls or thinking");
        });
    }
}
