using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LouisAgent.Web.Components.Staging.Decks;

/// <summary>
/// A deck (F134.1): one item forward in the middle and the rest behind it in depth, with an arrow at each side. It
/// draws anything, through the caller's template, and knows nothing of what that is.
/// </summary>
/// <remarks>
/// <para>
/// A templated component (Microsoft's Blazor guidance): the caller says how one item is drawn, the deck says where it
/// stands. Louis: "like cool components we could reuse later in other projects". The showroom's start is the first
/// to use it, for its demos.
/// </para>
/// <para>
/// A carousel, as the WAI-ARIA Authoring Practices describe one: one item in view, a control before and after it,
/// and the keyboard's arrows. The page owns which item is forward: it comes in as a parameter and the deck only asks
/// for another, so the page can keep an address in step with it and nothing here can disagree with the page.
/// </para>
/// </remarks>
/// <typeparam name="TItem">What the deck holds.</typeparam>
/// <seealso href="https://learn.microsoft.com/en-us/aspnet/core/blazor/components/templated-components">Templated components (Microsoft)</seealso>
/// <seealso href="https://www.w3.org/WAI/ARIA/apg/patterns/carousel/">Carousel (WAI-ARIA Authoring Practices)</seealso>
public partial class Deck<TItem>
{
    /// <summary>What the deck holds, in order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Which item is forward.</summary>
    [Parameter]
    public int Forward { get; set; }

    /// <summary>Told which item the visitor asked to bring forward.</summary>
    [Parameter]
    public EventCallback<int> ForwardChanged { get; set; }

    /// <summary>How one item is drawn.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<TItem> Item { get; set; } = null!;

    /// <summary>What the deck is, for a screen reader.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>What the arrow to the item before says, for a screen reader.</summary>
    [Parameter, EditorRequired]
    public string BeforeLabel { get; set; } = "";

    /// <summary>What the arrow to the next item says, for a screen reader.</summary>
    [Parameter, EditorRequired]
    public string NextLabel { get; set; } = "";

    /// <summary>One item as the markup draws it: where it stands, and whether it is the one forward.</summary>
    private sealed record Slot(int Index, TItem Item, string Classes, string Place, string? Current, bool Behind);

    /// <summary>Every item with its place, in the deck's order.</summary>
    private IEnumerable<Slot> Slots => Items.Select((item, index) => SlotOf(item, index));

    /// <summary>
    /// One item's slot. Its distance goes into its style as two numbers the stylesheet turns into a place in depth,
    /// and the nearer it is the further up it is stacked.
    /// </summary>
    private Slot SlotOf(TItem item, int index)
    {
        int distance = DeckPlace.Of(index, Forward, Items.Count);
        int away = Math.Abs(distance);
        bool forward = distance == 0;

        string classes = forward ? "deck-item is-forward" : "deck-item";
        string place = FormattableString.Invariant($"--d: {distance}; --abs: {away}; z-index: {Items.Count - away};");

        return new Slot(index, item, classes, place, forward ? "true" : null, !forward);
    }

    /// <summary>An arrow: asks for the item one step to the right or left, coming round at either end.</summary>
    private Task StepAsync(int by) => ForwardChanged.InvokeAsync(DeckPlace.Moved(Forward, by, Items.Count));

    /// <summary>A press on an item: asks for it, unless it is the one forward already.</summary>
    private Task PressAsync(int index) => index == Forward ? Task.CompletedTask : ForwardChanged.InvokeAsync(index);

    /// <summary>The keyboard's arrows do what the deck's arrows do. Any other key is not the deck's.</summary>
    private Task KeyAsync(KeyboardEventArgs key) => key.Key switch
    {
        "ArrowRight" => StepAsync(1),
        "ArrowLeft" => StepAsync(-1),
        _ => Task.CompletedTask,
    };
}
