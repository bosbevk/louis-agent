using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

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
/// <para>
/// On a narrow screen there is no room for depth, so the same items are a row the visitor swipes, one at a time.
/// The styles make the row and the browser does the swiping. The deck's module, beside it (ADR 0093), only says
/// which item a swipe left in the middle, and a swipe then asks the page as an arrow does. On a wide screen the
/// same module leans the items a little towards the mouse. That is only how they look: it changes nothing the
/// page holds.
/// </para>
/// </remarks>
/// <typeparam name="TItem">What the deck holds.</typeparam>
/// <seealso href="https://learn.microsoft.com/en-us/aspnet/core/blazor/components/templated-components">Templated components (Microsoft)</seealso>
/// <seealso href="https://www.w3.org/WAI/ARIA/apg/patterns/carousel/">Carousel (WAI-ARIA Authoring Practices)</seealso>
public partial class Deck<TItem> : IAsyncDisposable
{
    private ElementReference _deck;
    private JsModule? _row;
    private DotNetObjectReference<Deck<TItem>>? _self;

    /// <summary>The item the row was last put at, or was swiped to; none before the deck is on the page.</summary>
    private int? _inView;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The deck's module, loaded when it is first called.</summary>
    private JsModule Row => _row ??= new JsModule(JS, FrameworkScripts.Deck);

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

    /// <summary>
    /// Once the deck is on the page its module watches the row. After that, each time the page puts another item
    /// forward, the row is brought to it.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender && Forward == _inView) return;

        // Noted before the browser is asked, so a redraw that comes while it answers doesn't ask again.
        _inView = Forward;
        if (!firstRender)
        {
            await Row.InvokeVoidAsync("bring", _deck, Forward);
            return;
        }

        _self = DotNetObjectReference.Create(this);
        await Row.InvokeVoidAsync("watch", _deck, _self, Forward);
    }

    /// <summary>
    /// The module's call: on a narrow screen the visitor swiped the row, and this item came to rest in the middle.
    /// The deck asks the page for it, as an arrow would. The row is there already, so it isn't brought to it again.
    /// </summary>
    [JSInvokable]
    public Task Viewed(int index)
    {
        if (index == Forward || index < 0 || index >= Items.Count) return Task.CompletedTask;

        _inView = index;
        return ForwardChanged.InvokeAsync(index);
    }

    /// <summary>The keyboard's arrows do what the deck's arrows do. Any other key is not the deck's.</summary>
    private Task KeyAsync(KeyboardEventArgs key) => key.Key switch
    {
        "ArrowRight" => StepAsync(1),
        "ArrowLeft" => StepAsync(-1),
        _ => Task.CompletedTask,
    };

    /// <summary>
    /// Lets the module go with the deck. Its listeners are on the deck's own row, so they go with it too.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_row is not null) await _row.DisposeAsync();
        _self?.Dispose();
        GC.SuppressFinalize(this);
    }
}
