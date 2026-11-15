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
/// F134.5: in the bar layout a selected piece shows its chip over the box, as it does in the corner's panel, and its
/// close lets the piece go.
/// </summary>
public class PieceInTheBarTests
{
    /// <summary>The widget as a bar, over the stub API.</summary>
    private static (BunitContext Ctx, IRenderedComponent<ChatWidget> Cut) Bar()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        ctx.JSInterop.SetupModule(FrameworkScripts.Widget)
            .Setup<Page>("pageText").SetResult(new Page("/", "Home", "Your best employee.", []));
        var cut = ctx.Render<ChatWidget>(parameters => parameters.Add(w => w.Identity, "guide")
            .Add(w => w.Title, "Ask the guide").Add(w => w.Placeholder, "Ask…").Add(w => w.Layout, WidgetLayout.Bar));
        return (ctx, cut);
    }

    /// <summary>A piece selected on the page shows its chip in the bar, and the chip's close lets it go.</summary>
    [Test]
    public void ASelectedPiece_ShowsItsChipInTheBar_UntilItIsClosed()
    {
        var (ctx, cut) = Bar();
        using var _ = ctx;

        var watch = cut.FindComponent<PieceWatch>().Instance;
        cut.InvokeAsync(() => watch.ShownAsync("note-specials", "Know today's specials"));
        cut.WaitForState(() => cut.FindAll(".agent-bar .piece-chip").Count == 1, TimeSpan.FromSeconds(5));
        string chip = cut.Find(".agent-bar .piece-chip").TextContent;
        cut.Find(".agent-bar .piece-chip button").Click();

        Assert.Multiple(() =>
        {
            Assert.That(chip, Does.Contain("Know today's specials"));
            Assert.That(cut.FindAll(".piece-chip"), Is.Empty);
        });
    }
}
