using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Web.Components.Packs;
using LouisAgent.Web.Components.Screens.Table;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace LouisAgent.Web.Tests.Components.Packs;

/// <summary>
/// The list that says which component draws a screen: the default for any name nobody registered, and a registered one
/// where someone did. The last to register wins, so a demo's view beats a pack's.
/// </summary>
public class PackViewsTests
{
    private static readonly ScreenReply Usage = new("usage", "Usage", "What was used.", [], Searchable: false);

    /// <summary>A pack's own view, and all it does is say so.</summary>
    private sealed class ThePacksView : ComponentBase, IScreenView
    {
        [Parameter] public ScreenReply Screen { get; set; } = null!;

        /// <inheritdoc />
        protected override void BuildRenderTree(RenderTreeBuilder builder) =>
            builder.AddMarkupContent(0, $"<p class=\"packs-view\">{Screen.Title}</p>");
    }

    /// <summary>A demo's view of the same screen.</summary>
    private sealed class TheDemosView : ComponentBase, IScreenView
    {
        [Parameter] public ScreenReply Screen { get; set; } = null!;
    }

    /// <summary>A name nobody registered gets the plain table.</summary>
    [Test]
    public void AnUnknownName_GetsTheDefault() =>
        Assert.That(new PackViews().ScreenView("usage"), Is.EqualTo(typeof(ScreenTable)));

    /// <summary>A registered name gets its view, and another name still gets the plain table.</summary>
    [Test]
    public void ARegisteredName_GetsItsView()
    {
        var views = new PackViews().UseScreen<ThePacksView>("usage");

        Assert.That(views.ScreenView("usage"), Is.EqualTo(typeof(ThePacksView)));
        Assert.That(views.ScreenView("visits"), Is.EqualTo(typeof(ScreenTable)));
    }

    /// <summary>The last to register wins, which is how a demo beats a pack.</summary>
    [Test]
    public void TheLastToRegister_Wins()
    {
        var views = new PackViews().UseScreen<ThePacksView>("usage").UseScreen<TheDemosView>("usage");

        Assert.That(views.ScreenView("usage"), Is.EqualTo(typeof(TheDemosView)));
    }

    /// <summary>A screen's name reads the same whatever its case, as the catalogue's does.</summary>
    [Test]
    public void TheNamesCase_DoesNotMatter()
    {
        var views = new PackViews().UseScreen<ThePacksView>("usage");

        Assert.That(views.ScreenView("USAGE"), Is.EqualTo(typeof(ThePacksView)));
    }

    /// <summary>A screen with columns can always be drawn; one without needs its own view.</summary>
    [Test]
    public void AScreenWithNoColumns_NeedsItsOwnView()
    {
        var withColumns = Usage with { Columns = [new ScreenColumnReply("Name", "text")] };
        var views = new PackViews();

        Assert.That(views.CanDraw(withColumns), Is.True);
        Assert.That(views.CanDraw(Usage), Is.False);
        Assert.That(views.UseScreen<ThePacksView>("usage").CanDraw(Usage), Is.True);
    }

    /// <summary>What it hands back draws through DynamicComponent, with the screen as its parameter.</summary>
    [Test]
    public void TheView_DrawsWithTheScreen()
    {
        using var ctx = new BunitContext();
        var views = new PackViews().UseScreen<ThePacksView>("usage");

        var drawn = ctx.Render<DynamicComponent>(p => p
            .Add(c => c.Type, views.ScreenView("usage"))
            .Add(c => c.Parameters, new Dictionary<string, object> { [nameof(IScreenView.Screen)] = Usage }));

        Assert.That(drawn.Find(".packs-view").TextContent, Is.EqualTo("Usage"));
    }
}
