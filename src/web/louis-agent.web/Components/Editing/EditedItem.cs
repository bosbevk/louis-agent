using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Editing;

/// <summary>
/// An item being edited, as its fields get it (F142): the item as it stands, and the way to hand back a changed
/// copy of it. A form that keeps or drops an item's edits as one hands this to the fields it draws.
/// </summary>
/// <param name="Item">The item as it stands, edits so far included.</param>
/// <param name="Changed">Hands back the item with one thing changed.</param>
public sealed record EditedItem<TItem>(TItem Item, EventCallback<TItem> Changed);
