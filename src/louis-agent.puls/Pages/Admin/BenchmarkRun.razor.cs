using System.Net;
using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Checks;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Notes;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Storage;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Pages.Admin;

/// <summary>
/// One stored run: its head, notes, roll-up, explorer, agreement, rates, trends and settings, read from the store. The
/// page reads the run as the contract's <c>StoredRunReply</c>; its panels still take the same JSON until each moves to
/// the records (the unattended run's finding 6).
/// </summary>
/// <remarks>
/// ADR 0042: after a grade or a note the page fetches the run again, so every panel shows what the store holds; the
/// explorer keeps its open conversation.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class BenchmarkRun : IDisposable
{
    private JsonElement? _json, _trend;
    private StoredRunReply? _run;
    private Dictionary<string, string> _labels = [];
    private string? _title, _check, _error;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatStore Store { get; set; } = null!;
    [Inject] private CurrencyState Currency { get; set; } = null!;
    [Inject] private ReviewerState Reviewer { get; set; } = null!;
    [Inject] private RunNotesState NoteState { get; set; } = null!;

    /// <summary>The run to show.</summary>
    [Parameter]
    public string RunId { get; set; } = "";

    /// <summary>A task a category's line linked to (`?task=`): its conversation opens in the explorer.</summary>
    [SupplyParameterFromQuery(Name = "task")]
    public string? FocusTask { get; set; }

    /// <summary>The run's address in the API.</summary>
    private string RunUrl => $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}";

    /// <summary>Starts listening for a grade or a note being saved, so the run is fetched again when one is.</summary>
    protected override void OnInitialized()
    {
        Reviewer.Changed += Refetch;
        NoteState.Changed += Refetch;
    }

    /// <summary>Fetches the run, its check's title and its trend, and shows an error if the run isn't there.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (!Store.Loaded) await Store.LoadAsync();
        Currency.Changed += StateHasChanged;
        await Currency.LoadAsync();
        _json = null;
        _run = null;
        _error = null;
        try
        {
            Read(await Api.GetAsync<JsonElement>(RunUrl));
            var list = (await Api.GetAsync<JsonElement>("v1/admin/benchmarks"))
                .Deserialize<BenchmarkListReply>(AdminJson.SnakeCase);
            _check = _run!.Run.Check;
            _title = list?.Checks.FirstOrDefault(c => c.Id == _check)?.Title;
            _labels = ModeLabels.Of(_run.Turns);
            _trend = await TrendAsync(_check);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Status == HttpStatusCode.NotFound ? $"No run {RunId} in the store." : ex.Message;
        }
    }

    /// <summary>Stops listening.</summary>
    public void Dispose()
    {
        Currency.Changed -= StateHasChanged;
        Reviewer.Changed -= Refetch;
        NoteState.Changed -= Refetch;
    }

    /// <summary>Keeps the run as the record the page reads and as the JSON its panels still take.</summary>
    private void Read(JsonElement json)
    {
        _json = json;
        _run = json.Deserialize<StoredRunReply>(AdminJson.SnakeCase);
    }

    /// <summary>The check's trend; none when it can't be read, and the page shows no trend.</summary>
    private async Task<JsonElement?> TrendAsync(string check)
    {
        try
        {
            return await Api.GetAsync<JsonElement>($"v1/admin/benchmarks/checks/{Uri.EscapeDataString(check)}/trend");
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
            Read(await Api.GetAsync<JsonElement>(RunUrl));
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }

        StateHasChanged();
    });
}
