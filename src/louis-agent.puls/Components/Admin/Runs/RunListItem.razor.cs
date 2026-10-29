using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Runs;

/// <summary>One stored run's row in the Benchmarks page (F18 step 5b.2).</summary>
public partial class RunListItem
{
    [Parameter, EditorRequired] public RunSummary Run { get; set; } = null!;

    [Parameter] public CurrencyChoice? Currency { get; set; }

    /// <summary>Another run is running, so a re-score cannot start (one run at a time).</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The id of the new run, once a re-score was started.</summary>
    [Parameter]
    public EventCallback<string> OnRescored { get; set; }

    private string StatusText => Run.Status switch { "running" => "running", "done" => "done", _ => "failed" };

    // The ledger's costs are USD; shown in the browser's currency at the app's rate (F14), as the chat does. A 0 is a known $0 (a re-score), the dash an unknown price.
    private string Cost => Run.Total?.Cost is { } cost ? UsageFormat.Money(cost, "USD", Currency) : "—";
}
