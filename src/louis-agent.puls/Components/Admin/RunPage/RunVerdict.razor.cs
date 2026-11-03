using LouisAgent.Puls.Admin.Verdict;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.RunPage;

/// <summary>The verdict card on top of the run page: the numbers of <see cref="RunVerdictView" /> placed.</summary>
public partial class RunVerdict
{
    /// <summary>The numbers to show.</summary>
    [Parameter, EditorRequired]
    public RunVerdictView View { get; set; } = null!;

    /// <summary>The currency the cost is shown in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }
}
