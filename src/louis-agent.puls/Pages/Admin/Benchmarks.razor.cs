using System.Net;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Puls.Admin.Starting;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Pages.Admin;

/// <summary>
/// The Benchmarks page: the store's state, a card per check, the run in progress and the chosen check's stored runs.
/// It reads the list as the contract's <c>BenchmarkListReply</c> and a run as <c>LiveRunRow</c>; the cards' trends
/// still go to the chart as the API's JSON (the unattended run's finding 6).
/// </summary>
/// <remarks>
/// ADR 0042: after a run ends the page fetches the list again, so the cards and the runs show what the store holds.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class Benchmarks : IDisposable
{
    private const string CurrentRunUrl = "v1/admin/benchmarks/runs/current";

    private readonly Dictionary<string, JsonElement> _trends = [];
    private BenchmarkListReply? _data;
    private IReadOnlyList<RunSummary> _runs = [];
    private LiveRunRow? _run;
    private bool _fromStore;
    private ManualReply? _manual;
    private string? _error, _selected;
    private bool _needsKey, _loading;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private CurrencyState Currency { get; set; } = null!;

    /// <summary>A run is going, so no other starts.</summary>
    private bool Running => _run?.Status == "running";

    /// <summary>The check whose runs are listed, once one is chosen.</summary>
    private CheckRow? Chosen => _data?.Checks.FirstOrDefault(c => c.Id == _selected);

    /// <summary>Reads the list, then picks up a run that was going before the page opened.</summary>
    protected override async Task OnInitializedAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        // Costs in the browser's currency (F14), as everywhere in Puls; the ledger's are USD.
        Currency.Changed += StateHasChanged;
        await Currency.LoadAsync();
        await LoadAsync();
        await ResumeAsync();
    }

    /// <summary>Stops listening for the currency.</summary>
    public void Dispose() => Currency.Changed -= StateHasChanged;

    /// <summary>Reads the checks, the store's state and the stored runs, then each check's trend.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _data = await Api.GetAsync<BenchmarkListReply>("v1/admin/benchmarks", AdminJson.SnakeCase);
            _runs = _data.Runs.Select(RunSummary.Of).ToList();
            _selected ??= RunEstimate.DefaultCheck(_data.Checks.Select(c => c.Id).ToList(), _runs);
        }
        catch (AgentApiException ex)
        {
            // A refused key shows the box to enter one.
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _error = ex.Message;
        }

        await LoadTrendsAsync();
        _loading = false;
    }

    /// <summary>Each check's trend for its card (step 11a); a check without finished runs draws nothing.</summary>
    private async Task LoadTrendsAsync()
    {
        if (_data is not { Store.Configured: true }) return;
        foreach (var check in _data.Checks)
        {
            try
            {
                _trends[check.Id] = await Api.GetAsync<JsonElement>(
                    $"v1/admin/benchmarks/checks/{Uri.EscapeDataString(check.Id)}/trend");
            }
            catch (AgentApiException)
            {
                _trends.Remove(check.Id);
            }
        }
    }

    /// <summary>
    /// A run started before this page opened (a reload): the panel reads its finished turns from the store, then
    /// follows its events. A judge's calibration run is the runner's, not this page's, and is left alone.
    /// </summary>
    private async Task ResumeAsync()
    {
        if (_data is null) return;
        try
        {
            var current = await LiveAsync();
            if (current is not { Status: "running", CalibrationOf: null }) return;
            Follow(current, fromStore: true);
        }
        catch (AgentApiException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            // No run yet.
        }
    }

    /// <summary>A card's Run button: start the run, list its check's runs, and follow it.</summary>
    private async Task StartAsync(StartRequest request)
    {
        _error = null;
        try
        {
            var started = await Api.PostAsync($"v1/admin/benchmarks/{request.Check}/runs",
                StartRunBody.Of(request));
            _selected = request.Check;
            Follow(started.Deserialize<LiveRunRow>(AdminJson.SnakeCase)!, fromStore: false);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
    }

    /// <summary>
    /// A re-score was started from a run's row (F18 step 5b.2): follow it like any run, from the API's current one.
    /// </summary>
    private async Task FollowRescoreAsync(string id)
    {
        _error = null;
        try
        {
            Follow((await LiveAsync())!, fromStore: false);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
    }

    /// <summary>The run the runner holds now.</summary>
    private Task<LiveRunRow?> LiveAsync() => Api.GetAsync<LiveRunRow?>(CurrentRunUrl, AdminJson.SnakeCase);

    /// <summary>Hands a run to the panel, which follows it.</summary>
    private void Follow(LiveRunRow run, bool fromStore)
    {
        _run = run;
        _fromStore = fromStore;
    }

    /// <summary>
    /// The run has ended: keep how it ended, and read the list again so the cards and the runs show it.
    /// </summary>
    private async Task RunEndedAsync(LiveRunRow ended)
    {
        _run = ended;
        await LoadAsync();
    }

    /// <summary>Shows what the API refused.</summary>
    private void ShowError(string message) => _error = message;

    /// <summary>Lists a check's runs.</summary>
    private void Select(string check) => _selected = check;

    /// <summary>What a check proves, as the list names it.</summary>
    private string? TitleOf(string check) => _data?.Checks.FirstOrDefault(c => c.Id == check)?.Title;

    /// <summary>A check's manual in a dialog (step 10b).</summary>
    private async Task OpenManualAsync(string check)
    {
        try
        {
            _manual = await Api.GetAsync<ManualReply?>(
                $"v1/admin/benchmarks/{Uri.EscapeDataString(check)}/manual", AdminJson.SnakeCase);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
    }

    /// <summary>Closes the manual.</summary>
    private void CloseManual() => _manual = null;
}
