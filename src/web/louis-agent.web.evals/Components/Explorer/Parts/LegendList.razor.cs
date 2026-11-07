using LouisAgent.Web.Evals.Admin.Explorer.Legends;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.Explorer.Parts;

/// <summary>Draws a chart's legend from its entries.</summary>
public partial class LegendList
{
    /// <summary>The legend's entries, in order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<LegendEntry> Entries { get; set; } = [];
}
