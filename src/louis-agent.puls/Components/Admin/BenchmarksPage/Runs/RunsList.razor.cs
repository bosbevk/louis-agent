using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.BenchmarksPage.Runs;

/// <summary>
/// The chosen check's stored runs, newest first, with a filter by model and what they cost together; each run can be
/// opened or re-scored.
/// </summary>
public partial class RunsList
{
    private string? _model;
    private CheckRuns _list = new([], [], [], null);

    /// <summary>The check whose runs to list; none until one is chosen.</summary>
    [Parameter]
    public CheckRow? Check { get; set; }

    /// <summary>Every stored run, newest first.</summary>
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

    /// <summary>The model the list is filtered to (F18 step 2a); none shows every run.</summary>
    private string? Model
    {
        get => _model;
        set
        {
            _model = value;
            _list = CheckRuns.For(Check?.Id, Runs, _model);
        }
    }

    /// <summary>Works the list out again for the check and runs handed down.</summary>
    protected override void OnParametersSet() => _list = CheckRuns.For(Check?.Id, Runs, _model);
}
