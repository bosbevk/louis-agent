using LouisAgent.Puls.Admin.Evals;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage;

/// <summary>
/// The runs before the latest, as a timeline: the same small bar per mode on every run, so you can see at a glance
/// which went well. Each run opens its own page.
/// </summary>
public partial class EvalRunTimeline
{
    /// <summary>The runs before the latest, newest first, as the card's view worked them out.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RunLine> Runs { get; set; } = [];

    /// <summary>The browser's currency for the costs.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }
}
