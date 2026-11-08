using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
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
}
