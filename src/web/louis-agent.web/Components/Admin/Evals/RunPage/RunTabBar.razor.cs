using LouisAgent.Web.Admin.RunPage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Evals.RunPage;

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
    private string Href(RunTab tab) => RunAddress.Of(RunId, tab, null);
}
