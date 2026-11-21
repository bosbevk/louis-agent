using System.Net;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Web.Evals.Admin.Evals.List;
using LouisAgent.Web.Evals.Admin.RunPage;
using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Evals.Components.LiveRun;

/// <summary>
/// A strip in the admin layout that says a run is going, wherever the reader is. It reads the runner's current run
/// when it opens, then again at a slow pace while that run goes, and shows nothing once it has ended.
/// </summary>
/// <remarks>
/// Polling at a slow rate, not the event stream: the strip only has to say "a run is going", and the panel that
/// follows the run on the Evals page keeps the stream. The layout lives across the admin pages, so one strip serves
/// them all.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0042-the-web-shows-the-live-results.md">ADR 0042: the web shows the live results</seealso>
public partial class LiveRunStrip : IDisposable
{
    /// <summary>The runner's last answer: the run going now, or one that has ended; none when it has none.</summary>
    private LiveRunRow? _run;

    /// <summary>Stops the polling when the strip goes.</summary>
    private readonly CancellationTokenSource _gone = new();

    /// <summary>The API the current run is read from.</summary>
    [Inject]
    private AgentApi Api { get; set; } = null!;

    /// <summary>How long the strip waits between two reads while a run goes.</summary>
    [Parameter]
    public TimeSpan Poll { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Reads the current run, and keeps reading while it goes.</summary>
    protected override async Task OnInitializedAsync()
    {
        _run = await ReadAsync();
        try
        {
            while (_run is { Status: "running" })
            {
                await Task.Delay(Poll, _gone.Token);
                _run = await ReadAsync();
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (OperationCanceledException)
        {
            // The strip went while a run was going.
        }
    }

    /// <summary>Stops the polling.</summary>
    public void Dispose() => _gone.Cancel();

    /// <summary>The runner's current run; none when it has none, or can't say.</summary>
    private async Task<LiveRunRow?> ReadAsync()
    {
        try
        {
            return await Api.GetAsync<LiveRunRow?>("v1/admin/benchmarks/runs/current", AdminJson.SnakeCase);
        }
        catch (AgentApiException)
        {
            return null;
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    /// <summary>Where the run is followed: a calibration's run page, else the eval's details.</summary>
    private static string Href(LiveRunRow run) =>
        run.CalibrationOf is { } of ? RunAddress.Of(of) : EvalAddress.Of(run.Check);
}
