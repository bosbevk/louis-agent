using LouisAgent.Puls.Admin.Runs;
using LouisAgent.Puls.Admin.Starting;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Puls.Components.Admin.Runs;

/// <summary>
/// The Replay button of a recorded run (FX28): it asks the page to run the check again with the run's repeats,
/// playing its answers back. Unlike <see cref="RescoreButton"/> it posts nothing itself: a replay is an ordinary run
/// on the page's one start path, which follows it as it follows any run.
/// </summary>
public partial class ReplayButton
{
    /// <summary>The recorded run to play back.</summary>
    [Parameter, EditorRequired]
    public RunSummary Run { get; set; } = null!;

    /// <summary>Another run is running: one at a time.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>What to start: the run's check at its repeats, replaying it.</summary>
    [Parameter]
    public EventCallback<StartRequest> OnStart { get; set; }

    /// <summary>Asks the page to start the replay.</summary>
    private Task StartAsync() => OnStart.InvokeAsync(new StartRequest(Run.Check, Run.Repeat, Run.Id));
}
