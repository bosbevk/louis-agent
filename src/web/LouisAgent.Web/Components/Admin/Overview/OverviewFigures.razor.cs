using LouisAgent.Api.Contracts.Admin.Overview;
using LouisAgent.Web.Components.Areas.Overview;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Overview;

/// <summary>
/// One period's figures on the admin's overview (F127.8): how many talks, what they cost, and how many talks called
/// each tool the site counts, under the name the site gave it (louis-agent.dev's SendLead as leads).
/// </summary>
public partial class OverviewFigures
{
    /// <summary>What the period is, such as Today.</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>The period's talks, cost and tools.</summary>
    [Parameter, EditorRequired]
    public OverviewPeriod Period { get; set; } = default!;

    /// <summary>The tools the site counts.</summary>
    [Parameter, EditorRequired]
    public OverviewCounts Counts { get; set; } = default!;

    /// <summary>The currency the reply's costs are in.</summary>
    [Parameter, EditorRequired]
    public string Currency { get; set; } = "";

    /// <summary>The reader's currency, when they chose one.</summary>
    [Parameter]
    public CurrencyChoice? Choice { get; set; }
}
