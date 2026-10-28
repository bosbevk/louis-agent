using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Evals.Protocol;
using LouisAgent.Puls.Admin.LiveRuns;
using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Shared.Usage;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.BenchmarksPage.LiveRun;

/// <summary>
/// The run in progress: it follows the run's events as they come, a row per turn with the open turn typing out, and
/// the script's plain lines folded under it. When the run ends it fetches each turn's cost and hands the finished run
/// back to the page.
/// </summary>
/// <remarks>
/// The panel owns the following (the stream, the table, the open turn), and the page only says which run and hears
/// when it has ended: state flows down as parameters and up as callbacks, and the page's own code keeps to the list
/// (the unattended run's finding 8).
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class LiveRunPanel
{
    /// <summary>The table of turns as the events come in.</summary>
    private readonly LiveTurns _turns = new();

    /// <summary>The runner's plain lines, the ones that are no event.</summary>
    private readonly List<string> _log = [];

    /// <summary>The run being followed, so the same run handed back isn't followed twice.</summary>
    private string? _followed;

    /// <summary>A render is already waiting, so the next event doesn't ask for another.</summary>
    private bool _renderPending;

    /// <summary>The API the page talks to.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The run to follow, and later the same run as it ended.</summary>
    [Parameter, EditorRequired]
    public LiveRunRow Run { get; set; } = null!;

    /// <summary>
    /// The run was going before the page opened, so its finished turns are read from the store first.
    /// </summary>
    [Parameter]
    public bool FromStore { get; set; }

    /// <summary>What the run's check proves, when the list names it; the check's id otherwise.</summary>
    [Parameter]
    public string? Title { get; set; }

    /// <summary>The currency the page shows money in.</summary>
    [Parameter]
    public CurrencyChoice? Currency { get; set; }

    /// <summary>The run has ended (or its events stopped), with its last state.</summary>
    [Parameter]
    public EventCallback<LiveRunRow> OnEnded { get; set; }

    /// <summary>Something the API refused, for the page to show.</summary>
    [Parameter]
    public EventCallback<string> OnError { get; set; }

    /// <summary>The run is still going.</summary>
    private bool Running => Run.Status == "running";

    /// <summary>The run's address in the API.</summary>
    private string RunUrl => $"v1/admin/benchmarks/runs/{Run.Id}";

    /// <summary>
    /// Starts following a run the first time the page hands it over; the same run handed back is left alone.
    /// </summary>
    protected override async Task OnParametersSetAsync()
    {
        if (Run.Id == _followed) return;
        _followed = Run.Id;
        await FollowAsync(Run);
    }

    /// <summary>Shows the run's lines as they arrive; when it ends, its costs per turn, then tells the page.</summary>
    private async Task FollowAsync(LiveRunRow run)
    {
        _turns.Start();
        if (FromStore) await ReadStoreAsync();
        _log.Clear();
        StateHasChanged();

        var ended = run;
        try
        {
            await foreach (var (type, line) in Api.StreamLinesAsync($"{RunUrl}/events"))
            {
                // The last event says how the run ended; the stream stops after it.
                if (type == "done")
                {
                    ended = JsonSerializer.Deserialize<LiveRunRow>(line, AdminJson.SnakeCase) ?? run;
                    break;
                }

                Take(line);
                RenderThrottled();
            }
        }
        catch (AgentApiException ex)
        {
            await OnError.InvokeAsync(ex.Message);
            return;
        }

        await CostsAsync();
        await OnEnded.InvokeAsync(ended);
    }

    /// <summary>
    /// One line of the runner: an event into the table, anything else into the plain lines. The contract's reader
    /// decides which: a line that is a JSON object with an <c>event</c> is an event (7c's pick B).
    /// </summary>
    private void Take(string line)
    {
        if (EvalProtocolJson.Read(line) is not { } e)
        {
            _log.Add(line);
            return;
        }

        // One bad event must not end the whole story: it goes to the browser's console, the rest goes on.
        try
        {
            _turns.Apply(e);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            Console.Error.WriteLine($"[admin] event of run {Run.Id} not applied: {ex.Message}");
        }
    }

    /// <summary>
    /// The turns the store already holds, so a reloaded page doesn't depend on the run's kept lines alone.
    /// </summary>
    private async Task ReadStoreAsync()
    {
        // The store may not have the run yet: then the events will tell.
        if (await StoredAsync() is { } stored) _turns.FromStore(stored);
    }

    /// <summary>
    /// Each turn's cost once the run has ended, from the store's requests; they stay "—" if it can't say.
    /// </summary>
    private async Task CostsAsync()
    {
        if (await StoredAsync() is { } stored) _turns.CostsFrom(stored);
    }

    /// <summary>The run as the store holds it, or nothing when the store can't say.</summary>
    private async Task<StoredRunReply?> StoredAsync()
    {
        try
        {
            return await Api.GetAsync<StoredRunReply?>(RunUrl, AdminJson.SnakeCase);
        }
        catch (AgentApiException)
        {
            return null;
        }
    }

    /// <summary>
    /// A turn opened from the table; one the store holds but this page never streamed is replayed from its events.
    /// </summary>
    private async Task OpenTurnAsync(TurnKey key)
    {
        if (_turns.Choose(key) is not { } row || row.Streamed) return;
        try
        {
            string url = $"{RunUrl}/turns/{Uri.EscapeDataString(key.Mode)}/{key.Repeat}/" +
                         $"{Uri.EscapeDataString(key.Task)}/{key.N}/events";
            var reply = await Api.GetAsync<TurnEventsReply?>(url, AdminJson.SnakeCase);
            row.Replay(reply?.Events ?? []);
        }
        catch (AgentApiException ex)
        {
            await OnError.InvokeAsync(ex.Message);
        }
    }

    /// <summary>A render at most every 80 ms while events pour in, as the chat does.</summary>
    private void RenderThrottled()
    {
        if (_renderPending) return;
        _renderPending = true;
        _ = Task.Delay(80).ContinueWith(_ => InvokeAsync(() =>
        {
            _renderPending = false;
            StateHasChanged();
        }));
    }
}
