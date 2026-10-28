using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Puls.Admin.Starting;
using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace LouisAgent.Puls.Components.Admin.BenchmarksPage.Checks;

/// <summary>
/// One check's card: its name and what it proves, its last run, its trend, and a Run button with the repeats to run
/// and roughly what that will cost. Clicking the card lists the check's runs below.
/// </summary>
public partial class CheckCard
{
    /// <summary>The repeats the switch offers.</summary>
    private static readonly int[] RepeatChoices = [1, 2, 3, 4, 5];

    /// <summary>The repeats chosen on the card's switch.</summary>
    private int _repeats = 1;

    /// <summary>The check.</summary>
    [Parameter, EditorRequired]
    public CheckRow Check { get; set; } = null!;

    /// <summary>The stored runs, newest first: the card shows its check's last one and estimates from them.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RunSummary> Runs { get; set; } = [];

    /// <summary>The check's trend, when it has one.</summary>
    [Parameter]
    public JsonElement? Trend { get; set; }

    /// <summary>The cards are unfolded, so the body shows.</summary>
    [Parameter]
    public bool Open { get; set; }

    /// <summary>This check's runs are the ones listed below.</summary>
    [Parameter]
    public bool Selected { get; set; }

    /// <summary>A run is going, so no other starts.</summary>
    [Parameter]
    public bool Running { get; set; }

    /// <summary>The store answers, so a run may start.</summary>
    [Parameter]
    public bool CanRun { get; set; }

    /// <summary>The currency the page shows money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>The card was clicked.</summary>
    [Parameter]
    public EventCallback<string> OnSelect { get; set; }

    /// <summary>The Manual button was clicked.</summary>
    [Parameter]
    public EventCallback<string> OnManual { get; set; }

    /// <summary>The Run button was clicked.</summary>
    [Parameter]
    public EventCallback<StartRequest> OnRun { get; set; }

    /// <summary>The check's newest run, if it has one.</summary>
    private RunSummary? Last => Runs.FirstOrDefault(r => r.Check == Check.Id);

    /// <summary>The stub check asks no model, so running it costs nothing.</summary>
    private bool Free => Check.Id == "stub";

    /// <summary>What a run at the repeats chosen will roughly cost, beside the Run button.</summary>
    private string Estimate => RunEstimate.Text(Check.Id, Runs, _repeats, cost => RunText.Money(cost, Currency));

    /// <summary>Lists this check's runs.</summary>
    private Task SelectAsync() => OnSelect.InvokeAsync(Check.Id);

    /// <summary>Enter or space on the card selects it, as a click does.</summary>
    private Task SelectKeyAsync(KeyboardEventArgs e) => e.Key is "Enter" or " " ? SelectAsync() : Task.CompletedTask;

    /// <summary>Opens the check's manual.</summary>
    private Task ManualAsync() => OnManual.InvokeAsync(Check.Id);

    /// <summary>Asks the page to run the check at the repeats chosen.</summary>
    private Task RunAsync() => OnRun.InvokeAsync(new StartRequest(Check.Id, _repeats));

    /// <summary>
    /// The trend's lines, read from the JSON the page still hands the card. It goes when the card takes the
    /// contract's trend (step 5, commit 12).
    /// </summary>
    private static IReadOnlyList<TrendLineRow> LinesOf(JsonElement trend) =>
        trend.Deserialize<TrendReply>(AdminJson.SnakeCase)?.Modes ?? [];
}
