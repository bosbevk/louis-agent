using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Editing.Inline;

/// <summary>
/// A small form for an item that is changed on its page and has more to it than its words (F142): a link has a
/// network, a name and an address. The edits are a draft of the item. The tick keeps them all, the cross drops them
/// all, and its owner hears nothing until the tick.
/// </summary>
/// <remarks>
/// The same inline edit as <see cref="InlineEdit"/>, for a value of several fields: a draft held here, confirmed as
/// one. The fields are the owner's to draw, from the row it is handed: the draft, and where to send a changed copy.
/// </remarks>
/// <typeparam name="TItem">What is changed: a record, so a changed copy is a new one.</typeparam>
public partial class InlineForm<TItem>
{
    private TItem _draft = default!;

    /// <summary>The item as its owner holds it.</summary>
    [Parameter, EditorRequired]
    public TItem Item { get; set; } = default!;

    /// <summary>What the item is, for a screen reader: link 2.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>Told the item with every edit, at the tick.</summary>
    [Parameter]
    public EventCallback<TItem> OnKeep { get; set; }

    /// <summary>Told the edits were dropped, at the cross.</summary>
    [Parameter]
    public EventCallback OnDrop { get; set; }

    /// <summary>The item's fields, drawn from the draft.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<EditedItem<TItem>> ChildContent { get; set; } = null!;

    /// <summary>The draft starts as the item, once: a redraw by the owner doesn't take the edits away.</summary>
    protected override void OnInitialized() => _draft = Item;

    /// <summary>The draft and where a field sends its changed copy.</summary>
    private EditedItem<TItem> Row =>
        new(_draft, EventCallback.Factory.Create<TItem>(this, changed => _draft = changed));

    /// <summary>Hands the owner the draft.</summary>
    private Task KeepAsync() => OnKeep.InvokeAsync(_draft);
}
