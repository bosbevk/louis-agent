using System.Text.Json;
using LouisAgent.Puls.Admin.Summary;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Summary;

/// <summary>A run's roll-up, per mode (F18 step 5c.3), read with <see cref="SummaryReader"/>.</summary>
public partial class SummaryPanel
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>The run as <c>GET /v1/admin/benchmarks/runs/{id}</c> answers it.</summary>
    [Parameter, EditorRequired] public JsonElement Run { get; set; }

    [Parameter] public CurrencyChoice? Currency { get; set; }

    private IReadOnlyList<ModeSlices>? Modes => SummaryReader.Read(Run);

    private string RunId => Run.GetProperty("run").GetProperty("id").GetString()!;

    /// <summary>The re-scored run is a new run: open its page, where its categories are.</summary>
    private void OpenRescored(string id) => Navigation.NavigateTo($"admin/benchmarks/{Uri.EscapeDataString(id)}");
}
