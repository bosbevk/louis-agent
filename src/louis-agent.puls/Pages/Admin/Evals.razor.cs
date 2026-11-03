using System.Net;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Evals;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Evals.List;
using LouisAgent.Puls.Admin.Starting;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Pages.Admin;

/// <summary>
/// The Evals page, the portal's front page: every eval the runner has in a list, the chosen one's details beside it,
/// and the run in progress. It reads the list as the contract's <c>EvalListReply</c> and a run as
/// <c>LiveRunRow</c>; a run started here, or going before the page opened, is followed by the live-run panel.
/// </summary>
/// <remarks>
/// ADR 0042: the page shows what it last fetched; Reload, and a run's end, fetch again. The eval that is open is in
/// the address (ADR 0065), so a link opens the same one and Back goes to the one before.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0065-the-view-is-in-the-address.md">ADR 0065: the view is in the address</seealso>
public partial class Evals : IDisposable
{
    /// <summary>The run in progress, as the API names it.</summary>
    private const string CurrentRunUrl = "v1/admin/benchmarks/runs/current";

    /// <summary>The list: the evals with their runs, and the store's state.</summary>
    private EvalListReply? _data;

    /// <summary>The run being followed, if any.</summary>
    private LiveRunRow? _run;

    /// <summary>The run was going before the page opened, so its turns come from the store first.</summary>
    private bool _fromStore;

    /// <summary>What went wrong, for the page to say.</summary>
    private string? _error;

    /// <summary>The API wants the admin key.</summary>
    private bool _needsKey;

    /// <summary>The list is being fetched.</summary>
    private bool _loading;

    /// <summary>The API the page reads.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The browser's store, loaded before the first read.</summary>
    [Inject]
    private ChatStore Store { get; set; } = null!;

    /// <summary>The currency the page shows money in.</summary>
    [Inject]
    private CurrencyState Currency { get; set; } = null!;

    /// <summary>The eval whose details are open (`?eval=`); none opens the eval that ran last.</summary>
    [SupplyParameterFromQuery(Name = "eval")]
    public string? Eval { get; set; }

    /// <summary>
    /// The eval the details pane shows: the one in the address when the list has it, else the default one.
    /// </summary>
    private EvalRow? Chosen => _data is { } data
        ? data.Evals.FirstOrDefault(e => e.Name == Eval) ??
          data.Evals.FirstOrDefault(e => e.Name == EvalListView.DefaultEval(data))
        : null;

    /// <summary>A run is going, so no other starts.</summary>
    private bool Running => _run?.Status == "running";

    /// <summary>Loads the admin key and the currency, then the list, then picks up a run that was going.</summary>
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

    /// <summary>Reads the evals and their runs; a refused key shows the box to enter one.</summary>
    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _data = await Api.GetAsync<EvalListReply>("v1/admin/evals", AdminJson.SnakeCase);
        }
        catch (AgentApiException ex)
        {
            _needsKey = ex.Status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden;
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _error = ex.Message;
        }

        _loading = false;
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

    /// <summary>The details' Run button, or a row's Replay button (FX28): start the run and follow it.</summary>
    private async Task StartAsync(StartRequest request)
    {
        _error = null;
        try
        {
            var started = await Api.PostAsync($"v1/admin/benchmarks/{request.Check}/runs",
                StartRunBody.Of(request));
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

    /// <summary>The run has ended: keep how it ended, and read the list again so the details show it.</summary>
    private async Task RunEndedAsync(LiveRunRow ended)
    {
        _run = ended;
        await LoadAsync();
    }

    /// <summary>Shows what the API refused.</summary>
    private void ShowError(string message) => _error = message;

    /// <summary>What an eval proves, as the list names it, for the live-run panel's title.</summary>
    private string? ProvesOf(string eval) => _data?.Evals.FirstOrDefault(e => e.Name == eval)?.Proves;
}
