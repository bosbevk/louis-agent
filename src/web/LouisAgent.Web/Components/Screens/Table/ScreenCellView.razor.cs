using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Screens.Table;

/// <summary>Draws one cell of a pack's screen, from its kind and its value.</summary>
public partial class ScreenCellView
{
    /// <summary>What the column holds.</summary>
    [Parameter, EditorRequired] public ScreenCellKind Kind { get; set; }

    /// <summary>The value; null is what the pack doesn't know.</summary>
    [Parameter] public string? Value { get; set; }
}
