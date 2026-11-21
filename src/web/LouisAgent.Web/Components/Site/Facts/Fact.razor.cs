using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Facts;

/// <summary>
/// One fact of a <see cref="FactStrip"/>: the figure, what it counts, and any buttons that change it.
/// </summary>
/// <remarks>
/// Template Method, in Razor: the cell's layout is fixed here, and a site fills the figure and its words, plain or
/// edited in place.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class Fact
{
    /// <summary>The figure, such as "17".</summary>
    [Parameter, EditorRequired] public RenderFragment? Value { get; set; }

    /// <summary>What it counts, such as "years shipping software".</summary>
    [Parameter, EditorRequired] public RenderFragment? Label { get; set; }

    /// <summary>The buttons that change the fact, under its words.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
