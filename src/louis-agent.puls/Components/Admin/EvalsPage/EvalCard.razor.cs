using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Puls.Admin.Evals;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage;

/// <summary>
/// One eval's card: what it proves, how its latest run went, the runs before it, and what it checks. It hands each
/// part what <see cref="EvalCardView"/> worked out.
/// </summary>
public partial class EvalCard
{
    /// <summary>The eval as the API listed it.</summary>
    [Parameter, EditorRequired]
    public EvalRow Eval { get; set; } = null!;

    /// <summary>The browser's currency for the runs' costs.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>What the card shows, worked out again whenever the page hands it a fresh eval.</summary>
    private EvalCardView View { get; set; } = null!;

    /// <summary>Works out the card from the eval the page just gave it.</summary>
    protected override void OnParametersSet() => View = EvalCardView.Of(Eval);
}
