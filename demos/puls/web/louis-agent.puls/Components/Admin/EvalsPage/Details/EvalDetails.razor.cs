using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Puls.Admin.Evals;
using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Puls.Admin.Starting;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.EvalsPage.Details;

/// <summary>
/// The details pane of the Evals page: everything about the chosen eval, and the controls that run it. What the list
/// reply carries is placed through <see cref="EvalCardView" />; the trend is fetched here, once per eval; a run is
/// the page's to start and follow, so this pane only says what to start.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class EvalDetails
{
    /// <summary>The eval's trend; none when the API has none for it.</summary>
    private TrendReply? _trend;

    /// <summary>The eval the trend was fetched for, so another eval fetches its own.</summary>
    private string? _trendOf;

    /// <summary>The API the trend is read from.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The eval, as the list reply carries it.</summary>
    [Parameter, EditorRequired]
    public EvalRow Eval { get; set; } = null!;

    /// <summary>The currency money is shown in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>A run is going, so no other starts and no re-score either.</summary>
    [Parameter]
    public bool Running { get; set; }

    /// <summary>The store answers, so a run may start.</summary>
    [Parameter]
    public bool CanRun { get; set; }

    /// <summary>Says the eval's drafts changed, so the page reads the list again.</summary>
    [Parameter]
    public EventCallback OnChanged { get; set; }

    /// <summary>Run or Replay was pressed: what the page should start.</summary>
    [Parameter]
    public EventCallback<StartRequest> OnRun { get; set; }

    /// <summary>A re-score was started from a run's row, with the new run's id.</summary>
    [Parameter]
    public EventCallback<string> OnRescored { get; set; }

    /// <summary>What the pane shows of the list reply.</summary>
    private EvalCardView View { get; set; } = null!;

    /// <summary>The eval's stored runs as the table and the estimate read them, newest first.</summary>
    private IReadOnlyList<RunSummary> Runs { get; set; } = [];

    /// <summary>Builds the view and the runs, and fetches the trend when the eval isn't the one shown before.</summary>
    protected override async Task OnParametersSetAsync()
    {
        View = EvalCardView.Of(Eval);
        Runs = [.. Eval.Runs.Select(run => RunSummary.Of(run.Run))];
        if (_trendOf == Eval.Name) return;
        _trendOf = Eval.Name;
        _trend = await TrendAsync(Eval.Name);
    }

    /// <summary>The eval's trend; none when the API can't give one, and the pane shows no trend.</summary>
    private async Task<TrendReply?> TrendAsync(string eval)
    {
        try
        {
            return await Api.GetAsync<TrendReply?>($"v1/admin/evals/{Uri.EscapeDataString(eval)}/trend",
                AdminJson.SnakeCase);
        }
        catch (AgentApiException)
        {
            return null;
        }
    }
}
