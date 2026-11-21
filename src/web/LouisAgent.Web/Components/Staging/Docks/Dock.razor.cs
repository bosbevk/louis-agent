using LouisAgent.Choices;
using LouisAgent.Web.Components.Site.Tiles;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Staging.Docks;

/// <summary>
/// A dock (F134.1): a row of icon buttons, the current one showing its name, with a divider where the group changes.
/// It draws <see cref="DockItem"/>s and knows nothing of what they stand for.
/// </summary>
/// <remarks>
/// <para>
/// A general part of the framework's web (Louis: "like cool components we could reuse later in other projects"). The
/// showroom's start is the first to use it, for its demos.
/// </para>
/// <para>
/// The page owns which button is current, as it does for the deck: it comes in as an id, and the dock only says
/// which was pressed. So a dock and a deck drawn from the page's one choice can never disagree. Each button is a card
/// of the page, the framework's own way of giving a place an address (ADR 0107), so the dock needs no script.
/// </para>
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every place has its own address</seealso>
public partial class Dock
{
    /// <summary>The buttons, in order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<DockItem> Items { get; set; } = [];

    /// <summary>The id of the current one; none, and no button is current.</summary>
    [Parameter]
    public string? Current { get; set; }

    /// <summary>Told which button was pressed.</summary>
    [Parameter]
    public EventCallback<DockItem> Chosen { get; set; }

    /// <summary>What the dock is, for a screen reader.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>
    /// One button as the markup draws it: its classes, whether it is open, and whether a divider goes before it.
    /// </summary>
    private sealed record Slot(DockItem Item, string Classes, string Open, bool StartsGroup);

    /// <summary>Every button, each knowing whether its group differs from the one before it.</summary>
    private IEnumerable<Slot> Slots =>
        Items.Select((item, index) => SlotOf(item, index == 0 ? null : Items[index - 1]));

    /// <summary>
    /// One button's slot. A divider goes before it when there is a button before it in another group.
    /// </summary>
    private Slot SlotOf(DockItem item, DockItem? before)
    {
        bool current = item.Id == Current;
        bool startsGroup = before is not null && before.Group != item.Group;

        return new Slot(item, ClassesOf(item, current), current ? "true" : "false", startsGroup);
    }

    /// <summary>A button's classes: itself, its colour when it has one, and current when it is.</summary>
    private static string ClassesOf(DockItem item, bool current)
    {
        string classes = current ? "dock-item is-current" : "dock-item";

        return item.Accent == TileAccent.None ? classes : $"{classes} accent-{item.Accent.Wire()}";
    }
}
