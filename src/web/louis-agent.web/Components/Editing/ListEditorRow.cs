using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Editing;

/// <summary>
/// One row of a <see cref="ListEditor{TItem}"/>, as the row's own fields get it: the item, its place in the list, and
/// the way to hand back a changed copy of it.
/// </summary>
/// <param name="Item">The item this row edits.</param>
/// <param name="Index">Its place in the list, from 0.</param>
/// <param name="Changed">Hands the list editor this item's new value, which it puts in the item's place.</param>
public sealed record ListEditorRow<TItem>(TItem Item, int Index, EventCallback<TItem> Changed);
