using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Editing.Inline;

/// <summary>
/// The small buttons beside an item of a list that is edited on its page (F142): a step earlier, a step later, and
/// out. An item with more to it than its words (a link has an address too) gets a button that opens it. The tools
/// only ask. The list is its owner's to change.
/// </summary>
public partial class ItemTools
{
    /// <summary>What an item is called, for the buttons' names: fact, link.</summary>
    [Parameter, EditorRequired]
    public string Noun { get; set; } = "";

    /// <summary>The item's place in its list, from nought.</summary>
    [Parameter, EditorRequired]
    public int At { get; set; }

    /// <summary>How many items the list holds, so the last knows it is last.</summary>
    [Parameter, EditorRequired]
    public int Count { get; set; }

    /// <summary>Asked to move the item a step: minus one is earlier, one is later.</summary>
    [Parameter]
    public EventCallback<int> OnMove { get; set; }

    /// <summary>Asked to take the item out.</summary>
    [Parameter]
    public EventCallback OnRemove { get; set; }

    /// <summary>Asked to open the item. With nothing listening there is no such button.</summary>
    [Parameter]
    public EventCallback OnOpen { get; set; }

    /// <summary>The item's place as a person counts it.</summary>
    private int Place => At + 1;

    /// <summary>A step earlier.</summary>
    private Task EarlierAsync() => OnMove.InvokeAsync(-1);

    /// <summary>A step later.</summary>
    private Task LaterAsync() => OnMove.InvokeAsync(1);
}
