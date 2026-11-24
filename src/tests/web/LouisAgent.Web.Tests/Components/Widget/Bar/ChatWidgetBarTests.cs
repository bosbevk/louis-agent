using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Site.Slides;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Bar;

/// <summary>
/// F134.3: the chat widget has two layouts and one engine (Louis: "we should have like chatwidget layout type"). The
/// corner is today's; the bar stands at the foot of the page with the box always there to type in.
/// </summary>
public class ChatWidgetBarTests
{
    /// <summary>A context with what the widget asks for, over the stub API.</summary>
    private static BunitContext Context()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out _);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        return ctx;
    }

    /// <summary>The guide's widget in a layout.</summary>
    private static IRenderedComponent<ChatWidget> Render(BunitContext ctx, WidgetLayout layout) =>
        ctx.Render<ChatWidget>(parameters => parameters
            .Add(w => w.Identity, "guide")
            .Add(w => w.Title, "Ask the guide")
            .Add(w => w.Placeholder, "Your question…")
            .Add(w => w.Layout, layout));

    /// <summary>The corner is the default and draws as it always has: one button, folded.</summary>
    [Test]
    public void TheCorner_IsTheDefault_AndOneButtonFolded()
    {
        using var ctx = Context();

        var cut = ctx.Render<ChatWidget>(parameters => parameters.Add(w => w.Identity, "guide"));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("button.widget-button"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll(".agent-bar"), Is.Empty);
        });
    }

    /// <summary>
    /// The bar has no button to open it: the box is there from the start (F134's rule 3), and there is no corner.
    /// </summary>
    [Test]
    public void TheBar_HasTheBoxFromTheStart_AndNoCornerButton()
    {
        using var ctx = Context();

        var cut = Render(ctx, WidgetLayout.Bar);

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll(".agent-bar #composer-input"), Has.Count.EqualTo(1));
            Assert.That(cut.FindAll("button.widget-button"), Is.Empty);
            Assert.That(cut.FindAll(".widget-panel"), Is.Empty);
        });
    }

    /// <summary>
    /// On a page of slides the bar stands as the corner's button (FX247, Louis's pick), so a slide gets the whole
    /// screen; on a flowing page it stays the bar.
    /// </summary>
    [TestCase(PageLayout.Slides, 1, 0)]
    [TestCase(PageLayout.Flow, 0, 1)]
    public void TheBar_StandsAsTheCorner_OnAPageOfSlides(PageLayout page, int buttons, int bars)
    {
        using var ctx = Context();

        var cut = ctx.Render<ChatWidget>(parameters => parameters
            .AddCascadingValue(page)
            .Add(w => w.Identity, "guide")
            .Add(w => w.Layout, WidgetLayout.Bar));

        Assert.Multiple(() =>
        {
            Assert.That(cut.FindAll("button.widget-button"), Has.Count.EqualTo(buttons));
            Assert.That(cut.FindAll(".agent-bar #composer-input"), Has.Count.EqualTo(bars));
        });
    }
}
