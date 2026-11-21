using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Editing.Inline;

/// <summary>
/// The button under a list that is edited on its page (F142), which asks for one more item. It only asks. The list
/// is its owner's to add to.
/// </summary>
public partial class ItemAdd
{
    /// <summary>What it adds, in the list's own words: Add a fact.</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>Asked for one more item.</summary>
    [Parameter]
    public EventCallback OnAdd { get; set; }
}
