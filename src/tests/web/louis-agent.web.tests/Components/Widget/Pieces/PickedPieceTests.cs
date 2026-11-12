using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Widget.Pieces;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Pieces;

/// <summary>
/// F127.7, Louis: "each part on the page becomes a piece the agent can elaborate on, and focus on. like select it";
/// his picks: a click selects it, a chip in the chat, the guide explaining it at once. The widget's watch hears the
/// click (its module rings the piece), the guide opens and is asked about it, and every message carries the piece
/// until the chip is closed.
/// </summary>
public class PickedPieceTests
{
    /// <summary>The widget over the stub API, which keeps what was sent.</summary>
    private static (BunitContext Ctx, IRenderedComponent<ChatWidget> Cut, StubHandler Handler) Widget()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out var handler);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        ctx.JSInterop.SetupModule(FrameworkScripts.Widget)
            .Setup<Page>("pageText").SetResult(new Page("/", "Home", "Your best employee.", []));
        var cut = ctx.Render<ChatWidget>(parameters => parameters.Add(w => w.Identity, "guide")
            .Add(w => w.Title, "Ask the guide").Add(w => w.Greeting, "Hi.").Add(w => w.Placeholder, "Ask…"));
        return (ctx, cut, handler);
    }

    /// <summary>The bodies sent to the session's messages.</summary>
    private static List<string> Sent(StubHandler handler) =>
        handler.Requests.Where(r => r.Url.EndsWith("/messages")).Select(r => r.Body!).ToList();

    /// <summary>
    /// A piece picked opens the guide and asks about it, with the piece as the page's selection; the chip shows it, and
    /// the next message still carries it.
    /// </summary>
    [Test]
    public void APickedPiece_IsAskedAbout_AndStaysSelected()
    {
        var (ctx, cut, handler) = Widget();
        using var _ = ctx;

        cut.InvokeAsync(() =>
            cut.FindComponent<PieceWatch>().Instance.Pick("piece-it-answers", "It answers while you sleep"));
        cut.WaitForState(() => Sent(handler).Count == 1, TimeSpan.FromSeconds(10));
        cut.WaitForState(() => cut.FindAll("button.send").Count == 1, TimeSpan.FromSeconds(10));
        cut.Find("#composer-input").Input("and at weekends?");
        cut.Find("button.send").Click();
        cut.WaitForState(() => Sent(handler).Count == 2, TimeSpan.FromSeconds(10));

        var sent = Sent(handler);
        Assert.Multiple(() =>
        {
            Assert.That(sent[0], Does.Contain("Tell me about \\u0022It answers while you sleep\\u0022")
                .Or.Contain("Tell me about \\\"It answers while you sleep\\\""));
            Assert.That(sent[0],
                Does.Contain("\"selected\":{\"id\":\"piece-it-answers\",\"heading\":\"It answers while you sleep\"}"));
            Assert.That(cut.Find(".piece-chip").TextContent, Does.Contain("It answers while you sleep"));
            Assert.That(sent[1], Does.Contain("\"selected\":{\"id\":\"piece-it-answers\""), "it stays selected");
        });
    }

    /// <summary>Closing the chip lets the piece go: the next message carries no selection.</summary>
    [Test]
    public void ClosingTheChip_LetsThePieceGo()
    {
        var (ctx, cut, handler) = Widget();
        using var _ = ctx;

        cut.InvokeAsync(() =>
            cut.FindComponent<PieceWatch>().Instance.Pick("piece-it-answers", "It answers while you sleep"));
        cut.WaitForState(() => Sent(handler).Count == 1 && cut.FindAll("button.send").Count == 1,
            TimeSpan.FromSeconds(10));
        cut.Find(".piece-chip button").Click();
        cut.Find("#composer-input").Input("what does it cost?");
        cut.Find("button.send").Click();
        cut.WaitForState(() => Sent(handler).Count == 2, TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".piece-chip"), Is.Empty);
            Assert.That(Sent(handler)[1], Does.Not.Contain("\"selected\""));
        });
    }

    /// <summary>
    /// Louis: "the agent must be able to select each one going through the tour", "from anywhre that matter". A piece
    /// the guide showed is selected without a word sent, since the guide chose it: the chip shows it, and the visitor's
    /// next message carries it.
    /// </summary>
    [Test]
    public void APieceTheGuideShowed_IsSelected_WithNothingSent()
    {
        var (ctx, cut, handler) = Widget();
        using var _ = ctx;
        cut.Find("button.widget-button").Click();

        cut.InvokeAsync(() => cut.FindComponent<PieceWatch>().Instance.ShownAsync("100-adrs", "100+ ADRs"));
        bool quiet = Sent(handler).Count == 0;
        cut.Find("#composer-input").Input("what are those?");
        cut.Find("button.send").Click();
        cut.WaitForState(() => Sent(handler).Count == 1, TimeSpan.FromSeconds(10));

        Assert.Multiple(() =>
        {
            Assert.That(quiet, Is.True, "nothing sent when the guide selects");
            Assert.That(cut.Find(".piece-chip").TextContent, Does.Contain("100+ ADRs"));
            Assert.That(Sent(handler)[0], Does.Contain("\"selected\":{\"id\":\"100-adrs\""));
        });
    }
}
