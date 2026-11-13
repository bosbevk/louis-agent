using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Editing;

/// <summary>
/// A few fields of a form that belong together (F142), under a small heading: side by side where there is room, one
/// under the other where there isn't, a box of several lines taking the whole width.
/// </summary>
public partial class FieldGroup
{
    /// <summary>What the fields have in common, shown over them; none for a group that needs no name.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The fields.</summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = null!;
}
