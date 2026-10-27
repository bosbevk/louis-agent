using System.Text.Json;
using LouisAgent.Puls.Admin.Rates;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Rates;

/// <summary>A run's modes with their pass@k and pass^k, read with <see cref="ModeRatesReader"/>.</summary>
public partial class ModeRatesList
{
    /// <summary>The run as <c>GET /v1/admin/benchmarks/runs/{id}</c> answers it.</summary>
    [Parameter, EditorRequired]
    public JsonElement Run { get; set; }

    private IReadOnlyList<ModeRate> Rates => ModeRatesReader.Read(Run);

    /// <summary>A bar's width for a rate; empty for a rate no trial made (the number beside it is a dash).</summary>
    private static string Width(double? rate) => rate is { } value
        ? Math.Round(value * 100, 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + "%"
        : "0";
}
