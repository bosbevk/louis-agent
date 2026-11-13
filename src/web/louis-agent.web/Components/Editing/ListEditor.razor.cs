using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Editing;

/// <summary>
/// A list a person edits in a form: jobs, schools, skill groups. Each item is one line that opens on its own fields,
/// which the page supplies; the lines can be added to, removed and moved up or down.
/// </summary>
/// <remarks>
/// A templated component (Microsoft's Blazor guidance): the list's behaviour is here once, and the page writes only
/// the fields of one row. It never changes the list it was given. Each action hands back a new list through
/// <see cref="ItemsChanged"/>, so the page stays the one owner of what is being edited, and a list of records works
/// as it is. One row is open at a time, so a long list stays a list.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/aspnet/core/blazor/components/templated-components">Templated components (Microsoft Learn)</seealso>
public partial class ListEditor<TItem>
{
    /// <summary>The open row's place, or -1 when all are folded.</summary>
    private int _open = -1;

    /// <summary>The list as it stands.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<TItem> Items { get; set; } = [];

    /// <summary>Hands back the list after an add, a remove, a move or a row's change.</summary>
    [Parameter]
    public EventCallback<IReadOnlyList<TItem>> ItemsChanged { get; set; }

    /// <summary>Makes the blank item Add appends.</summary>
    [Parameter, EditorRequired]
    public Func<TItem> New { get; set; } = null!;

    /// <summary>What a row's line says: the item in a few words.</summary>
    [Parameter, EditorRequired]
    public Func<TItem, string?> TitleOf { get; set; } = null!;

    /// <summary>The quieter words beside the title, such as a job's dates; none, and there are none.</summary>
    [Parameter]
    public Func<TItem, string?>? NoteOf { get; set; }

    /// <summary>What one item is called, in lower case, for the buttons' names: "job" gives "Remove job 2".</summary>
    [Parameter, EditorRequired]
    public string Noun { get; set; } = "";

    /// <summary>What the add button says, such as "Add a job".</summary>
    [Parameter, EditorRequired]
    public string AddLabel { get; set; } = "";

    /// <summary>The fields of one row, given its item and the way to hand back a changed copy.</summary>
    [Parameter, EditorRequired]
    public RenderFragment<ListEditorRow<TItem>> ChildContent { get; set; } = null!;

    /// <summary>Whether the item has words of its own for its line yet.</summary>
    private bool HasTitle(TItem item) => !string.IsNullOrWhiteSpace(TitleOf(item));

    /// <summary>The line's words: the item's own, or "New job" for one not filled in yet.</summary>
    private string Title(TItem item) => HasTitle(item) ? TitleOf(item)! : $"New {Noun}";

    /// <summary>What a row's fields get: the item, its place, and where to hand its changed copy.</summary>
    private ListEditorRow<TItem> RowAt(int at) =>
        new(Items[at], at, EventCallback.Factory.Create<TItem>(this, changed => ReplaceAsync(at, changed)));

    /// <summary>Opens the row, or folds it when it is the open one.</summary>
    private void Toggle(int at) => _open = _open == at ? -1 : at;

    /// <summary>Appends a blank item and opens it, ready to fill in.</summary>
    private Task AddAsync()
    {
        _open = Items.Count;
        return ItemsChanged.InvokeAsync(Items.Added(New()));
    }

    /// <summary>Drops the item, and folds the list: the rows after it have moved up.</summary>
    private Task RemoveAsync(int at)
    {
        _open = -1;
        return ItemsChanged.InvokeAsync(Items.Without(at));
    }

    /// <summary>Swaps the item with the one a step up (-1) or down (1); the open row follows its item.</summary>
    private Task MoveAsync(int at, int step)
    {
        int to = at + step;
        if (to < 0 || to >= Items.Count) return Task.CompletedTask;

        _open = FollowTheSwap(_open, at, to);
        return ItemsChanged.InvokeAsync(Items.Moved(at, step));
    }

    /// <summary>Where the open row is after two rows swapped: with its item, if it was one of them.</summary>
    /// <remarks>Guard clauses, one per case (ADR 0051).</remarks>
    private static int FollowTheSwap(int open, int at, int to)
    {
        if (open == at) return to;
        if (open == to) return at;

        return open;
    }

    /// <summary>Puts a row's changed copy in its item's place.</summary>
    private Task ReplaceAsync(int at, TItem changed) =>
        ItemsChanged.InvokeAsync(Items.Replaced(at, changed));
}
