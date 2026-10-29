using LouisAgent.Puls.Admin.Explorer.Legends;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Explorer.Parts;

/// <summary>Draws a chart's legend from its entries.</summary>
public partial class LegendList
{
    /// <summary>The legend's entries, in order.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<LegendEntry> Entries { get; set; } = [];
}
