using System.Globalization;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Rates;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Rates;

/// <summary>A run's modes with their pass@k and pass^k, read with <see cref="ModeRatesReader"/>.</summary>
public partial class ModeRatesList
{
    /// <summary>The run's mode rows, one per repeat, as the contract's stored run reply carries them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<ModeRow> Modes { get; set; } = [];

    /// <summary>One line of rates per mode.</summary>
    private IReadOnlyList<ModeRate> Rates => ModeRatesReader.Read(Modes);

    /// <summary>A bar's width for a rate; empty for a rate no trial made (the number beside it is a dash).</summary>
    private static string Width(double? rate) => rate is { } value
        ? Math.Round(value * 100, 1).ToString(CultureInfo.InvariantCulture) + "%"
        : "0";
}
