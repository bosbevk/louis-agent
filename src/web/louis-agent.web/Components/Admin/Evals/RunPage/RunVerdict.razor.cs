using LouisAgent.Web.Admin.Verdict;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Evals.RunPage;

/// <summary>The verdict card on top of the run page: the numbers of <see cref="RunVerdictView" /> placed.</summary>
public partial class RunVerdict
{
    /// <summary>The run, for the link to the answers to grade.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>The numbers to show.</summary>
    [Parameter, EditorRequired]
    public RunVerdictView View { get; set; } = null!;

    /// <summary>The currency the cost is shown in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }
}
