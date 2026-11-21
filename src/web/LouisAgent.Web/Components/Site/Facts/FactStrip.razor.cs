using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Facts;

/// <summary>
/// A strip of a few facts on one of the theme's panes: each a figure big over what it counts, four across, two on a
/// phone, and one row on a printed page. A cell at its foot may hold a button that adds one more.
/// </summary>
/// <remarks>
/// Composite, in Razor: the strip lays out and draws the pane, and each <see cref="Fact"/> draws its figure and its
/// words, so a site gives only the facts.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class FactStrip
{
    /// <summary>The facts.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>What stands in the cell at the strip's foot, such as the button that adds a fact.</summary>
    [Parameter] public RenderFragment? Add { get; set; }
}
