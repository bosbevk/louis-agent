using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Puls.Admin.Starting;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Runs.Table;

/// <summary>
/// The eval's stored runs, newest first, with a filter by model and what they cost together; each run can be opened
/// or re-scored, a recorded one replayed (FX28).
/// </summary>
public partial class RunsList
{
    /// <summary>The model the filter is on; none shows every run.</summary>
    private string? _model;

    /// <summary>The runs as the filter leaves them, with the models and the cost together.</summary>
    private CheckRuns _list = new([], [], [], null);

    /// <summary>The eval whose runs to list.</summary>
    [Parameter, EditorRequired]
    public string Eval { get; set; } = "";

    /// <summary>The eval's stored runs, newest first.</summary>
    [Parameter, EditorRequired]
    public IReadOnlyList<RunSummary> Runs { get; set; } = [];

    /// <summary>A run is going, so no re-score starts.</summary>
    [Parameter]
    public bool Running { get; set; }

    /// <summary>The currency the page shows money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>A re-score was started, with the new run's id.</summary>
    [Parameter]
    public EventCallback<string> OnRescored { get; set; }

    /// <summary>A row's Replay button was clicked: what the page should start.</summary>
    [Parameter]
    public EventCallback<StartRequest> OnReplay { get; set; }

    /// <summary>The model the list is filtered to (F18 step 2a); none shows every run.</summary>
    private string? Model
    {
        get => _model;
        set
        {
            _model = value;
            _list = CheckRuns.For(Eval, Runs, _model);
        }
    }

    /// <summary>Works the list out again for the eval and runs handed down.</summary>
    protected override void OnParametersSet() => _list = CheckRuns.For(Eval, Runs, _model);
}
