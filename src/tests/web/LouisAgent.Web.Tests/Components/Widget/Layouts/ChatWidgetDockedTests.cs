using LouisAgent.Web.Api;
using LouisAgent.Web.Chats;
using LouisAgent.Web.Components.Widget;
using LouisAgent.Web.Components.Widget.Door;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Text;
using LouisAgent.Web.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace LouisAgent.Web.Tests.Components.Widget.Layouts;

/// <summary>
/// The docked layout (F164.2 step 3, Louis's pick C): the widget as a panel that stands in the place its page gives
/// it, beside the page, closed until something asks, and given back when closed. Below an extra-large screen,
/// where the page has no room beside it, the panel slides in from the side and a button opens it, Bootstrap's
/// responsive offcanvas (ADR 0085).
/// </summary>
public class ChatWidgetDockedTests
{
    /// <summary>
    /// A context with what the widget asks for, over the stub API, and a door for buttons to ask through.
    /// </summary>
    private static BunitContext Context(GuideDoor door) => Context(door, out _);

    /// <summary>The same context, with the stub that hears every call to the API.</summary>
    private static BunitContext Context(GuideDoor door, out StubHandler handler)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var settings = new ChatSettings();
        var api = FixtureApi.Client(settings, out handler);
        ctx.Services.AddSingleton(settings);
        ctx.Services.AddSingleton(api);
        ctx.Services.AddSingleton(new CurrencyState(api, ctx.JSInterop.JSRuntime));
        ctx.Services.AddSingleton(new ChatText());
        ctx.Services.AddSingleton(door);
        return ctx;
    }

    /// <summary>The widget, docked, with the framework Orbi's words.</summary>
    private static IRenderedComponent<ChatWidget> Docked(BunitContext ctx) =>
        ctx.Render<ChatWidget>(parameters => parameters
            .Add(widget => widget.Identity, "louis-agent-framework")
            .Add(widget => widget.Title, "Orbi · AI")
            .Add(widget => widget.Layout, WidgetLayout.Docked));

    /// <summary>
    /// Closed, it draws no panel, so its place shows what it always does; a narrow screen gets a button.
    /// </summary>
    [Test]
    public void Closed_ItDrawsNoPanel_AndAButtonOnlyForANarrowScreen()
    {
        using var ctx = Context(new GuideDoor());

        var cut = Docked(ctx);

        Assert.Multiple(() =>
        {
            Assert.That(cut.Find(".chat-widget").ClassList, Does.Contain("is-docked"));
            Assert.That(cut.FindAll(".widget-panel"), Is.Empty);
            Assert.That(cut.Find("button.widget-button").ClassList, Does.Contain("d-xl-none"),
                "the button shows only where the page has no room beside it");
        });
    }

    /// <summary>A question at the door opens the panel in its place, and it slides in where there is no room.</summary>
    [Test]
    public void AQuestionAtTheDoor_OpensThePanel_InItsPlace()
    {
        var door = new GuideDoor();
        using var ctx = Context(door);
        var cut = Docked(ctx);

        cut.InvokeAsync(() => door.Ask("Tell me about this page."));

        cut.WaitForAssertion(() =>
                Assert.That(cut.Find(".widget-panel").ClassList,
                    Is.SupersetOf(new[] { "is-docked", "offcanvas-xl", "offcanvas-end", "show" })),
            TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// The door opened with no question (F164.2 step 5, the header's Ask Orbi) opens the panel and asks nothing, so
    /// the visitor types the first words.
    /// </summary>
    [Test]
    public void TheDoorOpened_OpensThePanel_AndAsksNothing()
    {
        var door = new GuideDoor();
        using var ctx = Context(door, out var handler);
        var cut = Docked(ctx);

        cut.InvokeAsync(door.Open);

        cut.WaitForAssertion(() => Assert.That(cut.FindAll(".widget-panel"), Has.Count.EqualTo(1)),
            TimeSpan.FromSeconds(10));
        Assert.That(handler.Requests.Where(request => request.Method == HttpMethod.Post), Is.Empty,
            "no session opened, no message sent");
    }

    /// <summary>Opened by its button, where the page has no room beside it, its close gives the place back.</summary>
    [Test]
    public void ItsClose_GivesThePlaceBack()
    {
        using var ctx = Context(new GuideDoor());
        var cut = Docked(ctx);
        cut.Find("button.widget-button").Click();
        var opened = cut.FindAll(".widget-panel").Count;

        cut.Find(".widget-close").Click();

        Assert.Multiple(() =>
        {
            Assert.That(opened, Is.EqualTo(1), "its button opens the panel");
            Assert.That(cut.FindAll(".widget-panel"), Is.Empty);
        });
    }

    /// <summary>
    /// F169.3: docked and folded, its button is the agent's mark and the words it was given (Louis's pick 5), with no
    /// chat glyph; the corner's button, the one other sites fold to, keeps its glyph.
    /// </summary>
    [Test]
    public void DockedAndFolded_ItsButtonIsTheMarkAndItsWords_AndTheCornersKeepsItsGlyph()
    {
        using var ctx = Context(new GuideDoor());

        var docked = ctx.Render<ChatWidget>(parameters => parameters
            .Add(widget => widget.Title, "Orbi · AI")
            .Add(widget => widget.ButtonText, "Ask Orbi")
            .Add(widget => widget.Layout, WidgetLayout.Docked)).Find("button.widget-button");
        var corner = ctx.Render<ChatWidget>(parameters => parameters
            .Add(widget => widget.Title, "Orbi · AI")
            .Add(widget => widget.ButtonText, "Ask Orbi")).Find("button.widget-button");

        Assert.Multiple(() =>
        {
            Assert.That(docked.QuerySelectorAll(".orb"), Has.Length.EqualTo(1));
            Assert.That(docked.TextContent, Does.Contain("Ask Orbi"));
            Assert.That(docked.QuerySelector("svg"), Is.Null, "no chat glyph");
            Assert.That(corner.QuerySelector(".widget-icon svg"), Is.Not.Null, "the corner keeps its glyph");
            Assert.That(corner.QuerySelector(".orb"), Is.Null);
        });
    }

    /// <summary>Docked, it has no minimise or maximise: its size is its place's.</summary>
    [Test]
    public void Docked_ItHasNoWindowButtons()
    {
        var door = new GuideDoor();
        using var ctx = Context(door);
        var cut = Docked(ctx);

        cut.InvokeAsync(() => door.Ask("Tell me about this page."));

        cut.WaitForAssertion(() =>
                Assert.That(cut.Find(".widget-panel").QuerySelectorAll(".window-tool"), Is.Empty),
            TimeSpan.FromSeconds(10));
    }
}
