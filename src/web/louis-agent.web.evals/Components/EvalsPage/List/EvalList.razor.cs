using LouisAgent.Web.Evals.Admin.Evals.List;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.EvalsPage.List;

/// <summary>The Evals page's list pane: a line per eval, the chosen one marked.</summary>
public partial class EvalList
{
    /// <summary>The lines, in the catalogue's order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<EvalLine> Evals { get; set; } = [];

    /// <summary>The eval whose details are open.</summary>
    [Parameter]
    public string? Selected { get; set; }
}
