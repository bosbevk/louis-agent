using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components;

/// <summary>One named figure of a run, such as Took or Facts met.</summary>
public partial class RunFigure
{
    /// <summary>What the figure is.</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>Its value.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
