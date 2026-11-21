using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Api.Contracts.Admin.Trends;
using LouisAgent.Web.Evals.Admin.Explorer.Place;
using LouisAgent.Web.Evals.Admin.Notes;
using LouisAgent.Web.Evals.Admin.Reviews;
using LouisAgent.Web.Evals.Admin.RunPage;
using LouisAgent.Web.Evals.Admin.Runs;
using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using LouisAgent.Web.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Pages.Running;

/// <summary>
/// One stored run: its head and verdict, then one tab at a time (the summary, the explorer, the judge, the notes,
/// the settings), read from the store. The page reads the run as the contract's <c>StoredRunReply</c> and its check's
/// trend as <c>TrendReply</c>, and hands each panel the records it was written for.
/// </summary>
/// <remarks>
/// ADR 0042: after a grade or a note the page fetches the run again, so every panel shows what the store holds; the
/// explorer keeps its open conversation. The run is fetched when its id changes, not on every change of the address:
/// a tab is a link to the same run, and the page would otherwise flash "Loading…" and read the run again on each.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class BenchmarkRun : IDisposable
{
    /// <summary>The check's trend; none when it can't be read.</summary>
    private TrendReply? _trend;

    /// <summary>The run.</summary>
    private StoredRunReply? _run;

    /// <summary>A name per mode for the trend's legend.</summary>
    private Dictionary<string, string> _labels = [];

    /// <summary>What the run's check proves, when the list names it.</summary>
    private string? _title;

    /// <summary>The run's check.</summary>
    private string? _check;

    /// <summary>Why the run can't be shown.</summary>
    private string? _error;

    /// <summary>The run the page last fetched, so a change of tab doesn't fetch it again.</summary>
    private string? _fetched;

    /// <summary>Where the address says the explorer is, read once from its parts.</summary>
    private ExplorerPlace? _place;

    /// <summary>The API the page reads.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The browser's store, loaded before the first read.</summary>
    [Inject]
    private ChatStore Store { get; set; } = null!;

    /// <summary>The currency the page shows money in.</summary>
    [Inject]
    private CurrencyState Currency { get; set; } = null!;

    /// <summary>Says when a grade was saved.</summary>
    [Inject]
    private ReviewerState Reviewer { get; set; } = null!;

    /// <summary>Says when a note was saved.</summary>
    [Inject]
    private RunNotesState NoteState { get; set; } = null!;

    /// <summary>The browser's address, which carries the tab and the explorer's place.</summary>
    [Inject]
    private NavigationManager Navigation { get; set; } = null!;

    /// <summary>The run to show.</summary>
    [Parameter]
    public string RunId { get; set; } = "";

    /// <summary>The tab the address names (`?tab=`); the summary when it names none.</summary>
    [SupplyParameterFromQuery(Name = "tab")]
    public string? Tab { get; set; }

    /// <summary>The explorer's view (`?view=`): the run when the address names none.</summary>
    [SupplyParameterFromQuery(Name = "view")]
    public string? View { get; set; }

    /// <summary>The open conversation's task (`?task=`).</summary>
    [SupplyParameterFromQuery(Name = "task")]
    public string? FocusTask { get; set; }

    /// <summary>The open turn's number (`?turn=`).</summary>
    [SupplyParameterFromQuery(Name = "turn")]
    public int? FocusTurn { get; set; }

    /// <summary>The open request's time (`?request=`), in UTC round-trip form.</summary>
    [SupplyParameterFromQuery(Name = "request")]
    public string? FocusRequest { get; set; }

    /// <summary>The open request's mode (`?mode=`).</summary>
    [SupplyParameterFromQuery(Name = "mode")]
    public string? FocusMode { get; set; }

    /// <summary>Only the conversations still to review are listed (`?review=true`).</summary>
    [SupplyParameterFromQuery(Name = "review")]
    public bool Review { get; set; }

    /// <summary>The open tab, read once from the address.</summary>
    private RunTab ActiveTab => RunTabs.Parse(Tab);

    /// <summary>The run's address in the API.</summary>
    private string RunUrl => $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}";

    /// <summary>Starts listening for a grade or a note being saved, so the run is fetched again when one is.</summary>
    protected override void OnInitialized()
    {
        Reviewer.Changed += Refetch;
        NoteState.Changed += Refetch;
    }

    /// <summary>
    /// Fetches the run, its check's title and its trend when the run is another than the one shown, and shows an error
    /// if the run isn't there.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        _place = ExplorerPlace.Of(View, FocusTask, FocusTurn, FocusRequest, FocusMode, Review);
        if (_fetched == RunId) return;
        _fetched = RunId;
        if (!Store.Loaded) await Store.LoadAsync();
        Currency.Changed += StateHasChanged;
        await Currency.LoadAsync();
        _run = null;
        _error = null;
        try
        {
            _run = await ReadRunAsync();
            var list = await Api.GetAsync<BenchmarkListReply?>("v1/admin/benchmarks", AdminJson.SnakeCase);
            _check = _run.Run.Check;
            _title = list?.Checks.FirstOrDefault(c => c.Id == _check)?.Title;
            _labels = ModeLabels.Of(_run.Turns);
            _trend = await TrendAsync(_check);
        }
        // The API's own words, which name the run when the store doesn't hold it ("No stored run '…'.").
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
    }

    /// <summary>Stops listening.</summary>
    public void Dispose()
    {
        Currency.Changed -= StateHasChanged;
        Reviewer.Changed -= Refetch;
        NoteState.Changed -= Refetch;
    }

    /// <summary>Puts the explorer's new place in the address, so Back and a link bring it back.</summary>
    private void Moved(ExplorerPlace place) =>
        Navigation.NavigateTo(RunAddress.Of(RunId, RunTab.Conversations, place));

    /// <summary>Fetches the run from the store.</summary>
    private Task<StoredRunReply> ReadRunAsync() => Api.GetAsync<StoredRunReply>(RunUrl, AdminJson.SnakeCase);

    /// <summary>The check's trend; none when it can't be read, and the page shows no trend.</summary>
    private async Task<TrendReply?> TrendAsync(string check)
    {
        try
        {
            return await Api.GetAsync<TrendReply>($"v1/admin/benchmarks/checks/{Uri.EscapeDataString(check)}/trend",
                AdminJson.SnakeCase);
        }
        catch (AgentApiException)
        {
            return null;
        }
    }

    /// <summary>Fetches the run again and redraws, keeping whatever the explorer has open.</summary>
    private void Refetch() => _ = InvokeAsync(async () =>
    {
        try
        {
            _run = await ReadRunAsync();
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }

        StateHasChanged();
    });
}
