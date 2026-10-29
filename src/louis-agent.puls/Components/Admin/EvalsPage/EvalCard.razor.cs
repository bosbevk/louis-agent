using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage;

/// <summary>One eval's card: what it is, how it's graded, and its runs, or that it never ran.</summary>
public partial class EvalCard
{
    /// <summary>The eval as the API listed it.</summary>
    [Parameter, EditorRequired]
    public EvalRow Eval { get; set; } = null!;

    /// <summary>The browser's currency for the runs' costs.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }
}
