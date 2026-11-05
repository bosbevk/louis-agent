using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Puls.Admin.Starting;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage.Details;

/// <summary>
/// The controls that run an eval from its details: the repeats, a Run button that says roughly what the run will
/// cost, and the eval's manual in a dialog. The run itself is the page's to start, so one place follows it.
/// </summary>
/// <remarks>
/// State down, events up (the unattended run's finding 8): the page says whether a run may start and hears what to
/// start; the manual is this component's own, fetched when its button is pressed.
/// </remarks>
public partial class EvalRunControls
{
    /// <summary>The repeats the switch offers.</summary>
    private static readonly int[] RepeatChoices = [1, 2, 3, 4, 5];

    /// <summary>The repeats chosen on the switch.</summary>
    private int _repeats = 1;

    /// <summary>The manual open in the dialog, if any.</summary>
    private ManualReply? _manual;

    /// <summary>What the API refused when the manual was asked for.</summary>
    private string? _error;

    /// <summary>The API the manual is read from.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The eval's name, as the runner knows it.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The eval has a manual to open.</summary>
    [Parameter]
    public bool HasManual { get; set; }

    /// <summary>The eval's stored runs, newest first: the estimate comes from the last one that measured.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RunSummary> Runs { get; set; } = [];

    /// <summary>A run is going, so no other starts.</summary>
    [Parameter]
    public bool Running { get; set; }

    /// <summary>The store answers, so a run may start.</summary>
    [Parameter]
    public bool CanRun { get; set; }

    /// <summary>The currency money is shown in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>The Run button was pressed: what the page should start.</summary>
    [Parameter]
    public EventCallback<StartRequest> OnRun { get; set; }

    /// <summary>The stub asks no model, so running it costs nothing.</summary>
    private bool Free => Eval == "stub";

    /// <summary>What a run at the repeats chosen will roughly cost, beside the Run button.</summary>
    private string Estimate => RunEstimate.Text(Eval, Runs, _repeats, cost => RunText.Money(cost, Currency));

    /// <summary>Asks the page to run the eval at the repeats chosen.</summary>
    private Task RunAsync() => OnRun.InvokeAsync(new StartRequest(Eval, _repeats));

    /// <summary>Opens the eval's manual.</summary>
    private async Task ManualAsync()
    {
        _error = null;
        try
        {
            _manual = await Api.GetAsync<ManualReply?>(
                $"v1/admin/benchmarks/{Uri.EscapeDataString(Eval)}/manual", AdminJson.SnakeCase);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
    }

    /// <summary>Closes the manual.</summary>
    private void CloseManual() => _manual = null;
}
