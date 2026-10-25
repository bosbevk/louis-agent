using System.Text.Json;
using LouisAgent.Puls.Admin.Rates;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Rates;

/// <summary>A run's modes with their pass@k and pass^k, read with <see cref="ModeRatesReader"/>.</summary>
public partial class ModeRatesList
{
    /// <summary>The run as <c>GET /v1/admin/benchmarks/runs/{id}</c> answers it.</summary>
    [Parameter, EditorRequired] public JsonElement Run { get; set; }

    private IReadOnlyList<ModeRate> Rates => ModeRatesReader.Read(Run);
}
