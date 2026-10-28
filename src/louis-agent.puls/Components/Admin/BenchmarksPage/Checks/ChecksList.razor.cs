using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Puls.Admin.Starting;
using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.BenchmarksPage.Checks;

/// <summary>
/// The checks as cards, with one button that folds them all at once. The cards say what each check proves, how its
/// last run went, and start a run.
/// </summary>
public partial class ChecksList
{
    private bool _open = true;

    /// <summary>The checks, in the order the API lists them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<CheckRow> Checks { get; set; } = [];

    /// <summary>The stored runs, newest first.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RunSummary> Runs { get; set; } = [];

    /// <summary>Each check's trend, as the API sent it; a check without one draws no chart.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyDictionary<string, JsonElement> Trends { get; set; } = new Dictionary<string, JsonElement>();

    /// <summary>The check whose runs are listed below.</summary>
    [Parameter]
    public string? Selected { get; set; }

    /// <summary>A run is going, so no other starts.</summary>
    [Parameter]
    public bool Running { get; set; }

    /// <summary>The store answers, so a run may start.</summary>
    [Parameter]
    public bool CanRun { get; set; }

    /// <summary>The currency the page shows money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>A card was clicked: list its check's runs.</summary>
    [Parameter]
    public EventCallback<string> OnSelect { get; set; }

    /// <summary>A card's Manual button was clicked.</summary>
    [Parameter]
    public EventCallback<string> OnManual { get; set; }

    /// <summary>A card's Run button was clicked.</summary>
    [Parameter]
    public EventCallback<StartRequest> OnRun { get; set; }

    /// <summary>Folds or unfolds every card.</summary>
    private void Toggle() => _open = !_open;

    /// <summary>The check's trend, when it has one.</summary>
    private JsonElement? TrendOf(string check) => Trends.TryGetValue(check, out var trend) ? trend : null;
}
