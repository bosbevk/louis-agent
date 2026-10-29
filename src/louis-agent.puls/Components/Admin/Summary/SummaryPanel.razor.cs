using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Summary;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Summary;

/// <summary>A run's roll-up, per mode (F18 step 5c.3), read with <see cref="SummaryReader"/>.</summary>
public partial class SummaryPanel
{
    /// <summary>Opens the re-scored run's page.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    /// <summary>The run's summary rows as the contract's stored run reply carries them; none before 5c.</summary>
    [Parameter]
    public IReadOnlyList<SummaryRow>? Summaries { get; set; }

    /// <summary>The run they belong to, for the task links and the re-score.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>The currency the page shows money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>The run's slices per mode; none for a run from before 5c.</summary>
    private IReadOnlyList<ModeSlices>? Modes => SummaryReader.Read(Summaries);

    /// <summary>The re-scored run is a new run: open its page, where its categories are.</summary>
    private void OpenRescored(string id) => Navigation.NavigateTo($"admin/benchmarks/{Uri.EscapeDataString(id)}");
}
