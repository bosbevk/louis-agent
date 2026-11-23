using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Runs;

/// <summary>A row of a run's figures: what the page puts inside, laid out side by side.</summary>
public partial class RunFigures
{
    /// <summary>The figures.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
