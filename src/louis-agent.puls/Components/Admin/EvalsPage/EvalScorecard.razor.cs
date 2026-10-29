using LouisAgent.Puls.Admin.Evals;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage;

/// <summary>The latest run up top: how it went at a glance, a ring per mode.</summary>
public partial class EvalScorecard
{
    /// <summary>Puls's green for what passed; the rest is left clear so the ring's track shows through.</summary>
    private static readonly IReadOnlyList<string> RingColours = ["var(--green-light)", "transparent"];

    /// <summary>The latest run, as the card's view worked it out.</summary>
    [Parameter, EditorRequired]
    public RunLine Line { get; set; } = null!;

    /// <summary>The browser's currency for the run's cost.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }
}
