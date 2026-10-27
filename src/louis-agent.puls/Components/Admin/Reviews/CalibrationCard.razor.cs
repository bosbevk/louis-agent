using System.Text.Json;
using LouisAgent.Puls.Admin.Reviews;
using LouisAgent.Web.Shared.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Reviews;

/// <summary>
/// The judge check of one judge of a run (F18 step 6a.5c): the reports the API kept, newest first, and the button that starts a new one. A check is the runner's one run at a time, so
/// the card follows it through <c>runs/current</c> until it ends, then fetches the reports again: the page shows the live store (ADR 0042).
/// </summary>
public partial class CalibrationCard : IDisposable
{
    /// <summary>Cancelled when the card goes (6a.5e): a check left behind is still the API's run, but nothing here keeps looking at it.</summary>
    private readonly CancellationTokenSource _gone = new();

    [Inject] private AgentApi Api { get; set; } = null!;

    [Parameter, EditorRequired] public string RunId { get; set; } = "";

    /// <summary>The judge (the model grader) to check.</summary>
    [Parameter, EditorRequired] public string Grader { get; set; } = "";

    /// <summary>Whether anyone has passed or failed an answer of this judge: without a label there is nothing to check against.</summary>
    [Parameter] public bool HasLabels { get; set; }

    /// <summary>How long between looks at the running check; a test shortens it.</summary>
    [Parameter] public TimeSpan Poll { get; set; } = TimeSpan.FromSeconds(2);

    private IReadOnlyList<CalibrationReportView> _reports = [];
    private bool _loading = true, _running;
    private string? _error, _loadedFor;

    private CalibrationReportView? Latest => _reports.Count > 0 ? _reports[0] : null;

    private IReadOnlyList<CalibrationReportView> Earlier => _reports.Skip(1).ToList();

    protected override async Task OnParametersSetAsync()
    {
        if (_loadedFor == RunId) return;
        _loadedFor = RunId;
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _reports = CalibrationView.Of(await Api.GetAsync<JsonElement>($"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}/calibrations"));
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

    private async Task CheckAsync()
    {
        _error = null;
        _running = true;
        int before = _reports.Count;
        try
        {
            var row = await Api.PostAsync($"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}/calibrations/{Uri.EscapeDataString(Grader)}", new { });
            string id = row.GetProperty("id").GetString() ?? "";
            // The check runs in the API's runner; its row says when it has ended, and its last line why, when it failed (its lines are not streamed here).
            while (Text(row, "status") == "running" && Text(row, "id") == id)
            {
                await Task.Delay(Poll, _gone.Token);
                row = await Api.GetAsync<JsonElement>("v1/admin/benchmarks/runs/current");
                if (_gone.IsCancellationRequested) return;
            }
            if (Text(row, "status") == "failed") _error = CalibrationText.Failed(Text(row, "last_line"));
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

    private static string? Text(JsonElement e, string name) => e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>The check's time as the page shows times: the viewer's clock.</summary>
    private static string When(CalibrationReportView r) => r.At.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public void Dispose()
    {
        _gone.Cancel();
        _gone.Dispose();
    }
}
