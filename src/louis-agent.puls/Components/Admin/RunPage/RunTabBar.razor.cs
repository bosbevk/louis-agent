using LouisAgent.Choices;
using LouisAgent.Puls.Admin.RunPage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.RunPage;

/// <summary>The run page's tab bar: a link per tab, the open one lit.</summary>
public partial class RunTabBar
{
    /// <summary>The run the tabs belong to.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>The tab that is open.</summary>
    [Parameter]
    public RunTab Active { get; set; }

    /// <summary>The same run with this tab in the address.</summary>
    private string Href(RunTab tab) => $"admin/benchmarks/{Uri.EscapeDataString(RunId)}?tab={tab.Wire()}";
}
