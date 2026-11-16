using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Widget.Bar.Sides;

/// <summary>
/// Lets a site stand something in the empty room on each side of the agent bar, such as its name on the left and a
/// note on the right (FX168, Louis: "we need to show off some branding somewhere"). The site wraps its widget in it.
/// </summary>
/// <remarks>
/// A cascading value: the sides reach the bar through Blazor's cascade, so the widget between them, which knows
/// nothing of them, carries no parameter for them, and the bar draws them beside its own row, lined up with it
/// without copying its measures. Chosen over two slots threaded through the widget, which is at its size limit,
/// and over a second fixed layer, which would copy the bar's width and height to line up.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/aspnet/core/blazor/components/cascading-values-and-parameters">Cascading values and parameters (Microsoft)</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class BarSides
{
    /// <summary>What stands on the bar's left, such as the site's name.</summary>
    [Parameter]
    public RenderFragment? Start { get; set; }

    /// <summary>What stands on the bar's right, such as a note with a link.</summary>
    [Parameter]
    public RenderFragment? End { get; set; }

    /// <summary>The site's widget, whose bar draws the sides.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
