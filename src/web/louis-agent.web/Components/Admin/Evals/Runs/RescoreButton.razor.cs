using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Admin.Evals.Runs;

/// <summary>A stored run's Re-score button: it posts to the run's rescore route and reports the new run's id.</summary>
public partial class RescoreButton
{
    [Inject] private AgentApi Api { get; set; } = null!;

    /// <summary>The stored run to re-score.</summary>
    [Parameter, EditorRequired]
    public string RunId { get; set; } = "";

    /// <summary>Another run is running: one at a time.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>The id of the new run, once the API has started it.</summary>
    [Parameter]
    public EventCallback<string> OnStarted { get; set; }

    private bool _starting;
    private string? _error;

    /// <summary>Starts the re-score; a refusal says why.</summary>
    private async Task StartAsync()
    {
        _starting = true;
        _error = null;
        try
        {
            var started = await Api.PostAsync($"v1/admin/benchmarks/runs/{Uri.EscapeDataString(RunId)}/rescore",
                new { });
            await OnStarted.InvokeAsync(started.GetProperty("id").GetString());
        }
        catch (AgentApiException error)
        {
            _error = error.Message;
        }
        finally
        {
            _starting = false;
        }
    }
}
