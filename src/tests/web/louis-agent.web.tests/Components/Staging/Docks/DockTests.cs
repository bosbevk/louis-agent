using LouisAgent.Diagrams.Icons;
using LouisAgent.Web.Components.Site.Tiles;
using LouisAgent.Web.Components.Staging.Docks;

namespace LouisAgent.Web.Tests.Components.Staging.Docks;

/// <summary>
/// F134.1: under the stage is a dock, a button per demo with its icon, the current one showing its name. It is a part
/// of its own that draws plain data (Louis's pick of three: a record per button). Each button is a card of the page,
/// so the framework's addresses open it and no new script is needed for that.
/// </summary>
public class DockTests
{
    private static readonly DockItem[] Items =
    [
        new("bakery", "A bakery", DiagramIcon.Dining, TileAccent.Green, "first"),
        new("florist", "A florist", DiagramIcon.Home, TileAccent.Blue, "first"),
        new("coder", "A coder", DiagramIcon.Code, TileAccent.Red, "second"),
    ];

    /// <summary>What the dock last said was pressed; none until one is.</summary>
    private sealed class Pressed
    {
        public DockItem? Item { get; set; }
    }

    /// <summary>A dock of these buttons with this one current.</summary>
    private static IRenderedComponent<Dock> Dock(BunitContext ctx, string? current, Pressed? pressed = null,
        IReadOnlyList<DockItem>? items = null) =>
        ctx.Render<Dock>(parameters => parameters
            .Add(dock => dock.Items, items ?? Items)
            .Add(dock => dock.Current, current)
            .Add(dock => dock.Chosen, item => (pressed ?? new Pressed()).Item = item)
            .Add(dock => dock.Label, "All shops"));

    /// <summary>
    /// A button per item, in order, each with its icon, its name for a screen reader, its colour, and its id as a card
    /// of the page.
    /// </summary>
    [Test]
    public void EveryItem_IsAButton_WithItsIcon_ItsName_AndItsId()
    {
        using var ctx = new BunitContext();

        var dock = Dock(ctx, "florist");

        var buttons = dock.FindAll("nav.dock button.dock-item");
        Assert.Multiple(() =>
        {
            Assert.That(dock.Find("nav.dock").GetAttribute("aria-label"), Is.EqualTo("All shops"));
            Assert.That(buttons.Select(button => button.Id), Is.EqualTo(new[] { "bakery", "florist", "coder" }));
            Assert.That(buttons.Select(button => button.HasAttribute("data-card")), Is.All.True);
            Assert.That(buttons.Select(button => button.QuerySelectorAll("svg.line-icon").Length), Is.All.EqualTo(1));
            Assert.That(buttons.Select(button => button.QuerySelector(".dock-name")!.TextContent),
                Is.EqualTo(new[] { "A bakery", "A florist", "A coder" }));
            Assert.That(buttons.Select(button => button.GetAttribute("title")),
                Is.EqualTo(new[] { "A bakery", "A florist", "A coder" }));
            Assert.That(buttons[2].ClassList, Does.Contain("accent-red"));
        });
    }

    /// <summary>The current one is marked, and open as a card is. The rest are closed.</summary>
    [Test]
    public void TheCurrentOne_IsMarked_AndOpen()
    {
        using var ctx = new BunitContext();

        var buttons = Dock(ctx, "florist").FindAll("button.dock-item");

        Assert.Multiple(() =>
        {
            Assert.That(buttons.Select(button => button.ClassList.Contains("is-current")),
                Is.EqualTo(new[] { false, true, false }));
            Assert.That(buttons.Select(button => button.GetAttribute("aria-expanded")),
                Is.EqualTo(new[] { "false", "true", "false" }));
        });
    }

    /// <summary>With no current one, every button is closed.</summary>
    [Test]
    public void WithNoCurrentOne_EveryButtonIsClosed()
    {
        using var ctx = new BunitContext();

        var buttons = Dock(ctx, null).FindAll("button.dock-item");

        Assert.That(buttons.Select(button => button.GetAttribute("aria-expanded")),
            Is.EqualTo(new[] { "false", "false", "false" }));
    }

    /// <summary>
    /// A divider stands where the group changes, and nowhere else: not before the first, not within a group.
    /// </summary>
    [Test]
    public void ADivider_StandsWhereTheGroupChanges()
    {
        using var ctx = new BunitContext();

        var dock = Dock(ctx, "bakery");

        var parts = dock.FindAll("nav.dock > *").Select(part => part.ClassList.Contains("dock-divider")).ToList();
        Assert.Multiple(() =>
        {
            Assert.That(parts, Is.EqualTo(new[] { false, false, true, false }));
            Assert.That(dock.Find(".dock-divider").GetAttribute("aria-hidden"), Is.EqualTo("true"));
        });
    }

    /// <summary>Items with no groups have no dividers.</summary>
    [Test]
    public void WithNoGroups_ThereAreNoDividers()
    {
        using var ctx = new BunitContext();
        DockItem[] plain = [new("one", "One", DiagramIcon.Box), new("two", "Two", DiagramIcon.Box)];

        var dock = Dock(ctx, "one", items: plain);

        Assert.Multiple(() =>
        {
            Assert.That(dock.FindAll(".dock-divider"), Is.Empty);
            Assert.That(dock.Find("button.dock-item").ClassName, Is.EqualTo("dock-item is-current"));
        });
    }

    /// <summary>A press says which item it was. The dock changes nothing itself: the page decides.</summary>
    [Test]
    public void APress_SaysWhichItem()
    {
        using var ctx = new BunitContext();
        var pressed = new Pressed();
        var dock = Dock(ctx, "bakery", pressed);

        dock.Find("button#coder").Click();

        Assert.Multiple(() =>
        {
            Assert.That(pressed.Item, Is.EqualTo(Items[2]));
            Assert.That(dock.Find("button#bakery").ClassList, Does.Contain("is-current"), "the page decides");
        });
    }
}
