using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Reviews;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Puls.Admin.Starting;
using LouisAgent.Web.Shared.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Reviews;

/// <summary>
/// The judge check of one judge of a run (F18 step 6a.5c): the reports the API kept, newest first, and the button
/// that starts a new one. A check is the runner's one run at a time, so the card follows it through
/// <c>runs/current</c> until it ends, then fetches the reports again: the page shows the live store (ADR 0042).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0045-typed-answers-and-a-shared-contract.md">ADR 0045: typed answers and a shared contract</seealso>
public partial class CalibrationCard : IDisposable
{
    /// <summary>
    /// Cancelled when the card goes (6a.5e): a check left behind is still the API's run, but nothing here keeps
    /// looking at it.
    /// </summary>
    private readonly CancellationTokenSource _gone = new();

    /// <summary>The API the card talks to.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>The run whose judge is checked.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>The judge (the model grader) to check.</summary>
    [Parameter, EditorRequired]
    public string Grader { get; set; } = "";

    /// <summary>
    /// Whether anyone has passed or failed an answer of this judge: without a label there is nothing to check
    /// against.
    /// </summary>
    [Parameter]
    public bool HasLabels { get; set; }

    /// <summary>How long between looks at the running check; a test shortens it.</summary>
    [Parameter]
    public TimeSpan Poll { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>The reports the API kept, newest first.</summary>
    private IReadOnlyList<CalibrationReportView> _reports = [];

    /// <summary>The reports are still being fetched.</summary>
    private bool _loading = true;

    /// <summary>A check is running.</summary>
    private bool _running;

    /// <summary>What went wrong, for the card to say.</summary>
    private string? _error;

    /// <summary>The run whose reports are loaded, so the same run handed back isn't fetched twice.</summary>
    private string? _loadedFor;

    /// <summary>The newest report, if there is one.</summary>
    private CalibrationReportView? Latest => _reports.Count > 0 ? _reports[0] : null;

    /// <summary>The checks before the newest, folded.</summary>
    private IReadOnlyList<CalibrationReportView> Earlier => _reports.Skip(1).ToList();

    /// <summary>The run's address in the API.</summary>
    private string RunUrl => $"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}";

    /// <summary>Fetches the reports the first time a run is handed over.</summary>
    protected override async Task OnParametersSetAsync()
    {
        if (_loadedFor == RunId) return;
        _loadedFor = RunId;
        await LoadAsync();
    }

    /// <summary>Fetches the run's reports as the contract's reply.</summary>
    private async Task LoadAsync()
    {
        try
        {
            var reply = await Api.GetAsync<CalibrationsReply?>($"{RunUrl}/calibrations", AdminJson.SnakeCase);
            _reports = reply is null ? [] : CalibrationView.Of(reply);
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    /// <summary>Starts a check, follows it to its end, then fetches the reports again.</summary>
    private async Task CheckAsync()
    {
        _error = null;
        _running = true;
        int before = _reports.Count;
        try
        {
            var started = await Api.PostAsync($"{RunUrl}/calibrations/{Uri.EscapeDataString(Grader)}",
                NoBody.Instance);
            var row = started.Deserialize<LiveRunRow>(AdminJson.SnakeCase);
            string? id = row?.Id;

            // The check runs in the API's runner; its row says when it has ended, and its last line why, when it
            // failed (its lines are not streamed here).
            while (row is { Status: "running" } && row.Id == id)
            {
                await Task.Delay(Poll, _gone.Token);
                row = await Api.GetAsync<LiveRunRow?>("v1/admin/benchmarks/runs/current", AdminJson.SnakeCase);
                if (_gone.IsCancellationRequested) return;
            }

            if (row is { Status: "failed" }) _error = CalibrationText.Failed(row.LastLine);
            await LoadAsync();
            if (_error is null && _reports.Count == before) _error = CalibrationText.NoReportKept;
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
        catch (OperationCanceledException)
        {
            // The card went while the check ran: nothing to show.
        }
        finally
        {
            _running = false;
        }
    }

    /// <summary>The check's time as the page shows times: the viewer's clock.</summary>
    private static string When(CalibrationReportView r) => r.At.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    /// <summary>
    /// Cancels the wait. The source is not disposed: it has no timer, so there is nothing to free, and a disposed one
    /// makes the loop's next token throw an error instead of stopping quietly.
    /// </summary>
    public void Dispose() => _gone.Cancel();
}
