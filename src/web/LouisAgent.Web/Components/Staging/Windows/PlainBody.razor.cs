using LouisAgent.Diagrams.Icons;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Staging.Windows;

/// <summary>
/// A window's body for a thing with nothing of its own to show yet (F134.1): its icon, large, over its name and a
/// line. It gives every window a face, so a window is never empty.
/// </summary>
public partial class PlainBody
{
    /// <summary>The thing's icon.</summary>
    [Parameter, EditorRequired]
    public DiagramIcon Icon { get; set; }

    /// <summary>Its name.</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>The few words under its name.</summary>
    [Parameter, EditorRequired]
    public string Line { get; set; } = "";
}
