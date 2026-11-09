using LouisAgent.Packs.Triage.Web.Runs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Triage.Web.Demo;

/// <summary>
/// Plays a recorded triage run (F106.3.4): a step at a time, a reading beat apart, the stage lighting the agent at work
/// and the popup saying what it did and why; the steps gather in a list under it, and the site's ending shows once the
/// case is closed. A visitor can step back and forth, which stops the play, or play it again from the start.
/// </summary>
/// <remarks>
/// The run is the pack's own file, read by <see cref="TriageRunFile"/>, so a live run's steps arriving one by one will
/// play the same way when F106.3.3's workflow lands; this component only fetches, keeps where it is, and draws.
/// </remarks>
public partial class TriagePlayer : IDisposable
{
    /// <summary>How long a step stays on screen before the next, at a pace of 1: time to read its popup.</summary>
    private const int StepMs = 4500;

    private IReadOnlyList<TriageStepView>? _steps;
    private CancellationTokenSource? _stop;
    private int _at = -1;
    private int _played;
    private bool _ended;

    [Inject] private HttpClient Http { get; set; } = null!;

    /// <summary>Where the run is, from the site's own address: the pack's run file.</summary>
    [Parameter] public string Source { get; set; } = "";

    /// <summary>How fast it plays: 1 a reading beat a step, 0 no waits at all.</summary>
    [Parameter] public double Pace { get; set; } = 1;

    /// <summary>The site's words, English by default.</summary>
    [Parameter] public TriageWords Words { get; set; } = new();

    /// <summary>What shows when the case is closed: the site's offer.</summary>
    [Parameter] public RenderFragment? Ending { get; set; }

    /// <summary>The site's words about the run, at the top of the first column above the stage (F122).</summary>
    [Parameter] public RenderFragment? Intro { get; set; }

    /// <summary>The site's buttons, at the end of the controls' row once the case is closed (F122).</summary>
    [Parameter] public RenderFragment? Actions { get; set; }

    private IReadOnlyList<TriageStepView> Steps => _steps ?? [];

    private TriageStepView? Current => _at >= 0 && _at < Steps.Count ? Steps[_at] : null;

    private IEnumerable<TriageStepView> Shown => Steps.Take(_played);

    private bool AtStart => _at <= 0;

    private bool AtEnd => _at >= Steps.Count - 1;

    /// <summary>Starts playing once the player is on the page.</summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await PlayAsync();
    }

    /// <summary>Plays the run from its first step; a play already going stops first.</summary>
    private async Task PlayAsync()
    {
        _stop?.Cancel();
        _stop = new CancellationTokenSource();
        var stop = _stop.Token;
        _steps ??= await ReadAsync(stop);
        (_at, _played, _ended) = (-1, 0, false);

        // A step at a time, a reading beat apart; stepping back or forth stops it where it is.
        try
        {
            for (int next = 0; next < Steps.Count; next++)
            {
                (_at, _played) = (next, Math.Max(_played, next + 1));
                StateHasChanged();
                if (next < Steps.Count - 1) await Task.Delay((int)(StepMs * Pace), stop);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _ended = true;
        StateHasChanged();
    }

    /// <summary>Shows the step before, and stops the play.</summary>
    private void Back() => Move(-1);

    /// <summary>Shows the next step, and stops the play.</summary>
    private void Next() => Move(1);

    /// <summary>Shows the step a visitor picked on the swimlanes (F122), and stops the play.</summary>
    private void Pick(int step)
    {
        _stop?.Cancel();
        _at = Math.Clamp(step, 0, Math.Max(0, _played - 1));
    }

    /// <summary>The agents of the steps played so far, for the swimlanes.</summary>
    private IReadOnlyList<string> PlayedAgents => Shown.Select(step => step.Agent).ToList();

    /// <summary>Moves by one step within the steps played so far, the play stopped.</summary>
    private void Move(int by)
    {
        _stop?.Cancel();
        _at = Math.Clamp(_at + by, 0, Math.Max(0, Steps.Count - 1));
        _played = Math.Max(_played, _at + 1);
    }

    /// <summary>
    /// The run's steps; none when the site can't serve it, so the player goes straight to the ending.
    /// </summary>
    private async Task<IReadOnlyList<TriageStepView>> ReadAsync(CancellationToken stop)
    {
        try
        {
            using var response = await Http.GetAsync(Source, stop);
            if (!response.IsSuccessStatusCode) return [];
            return TriageRunFile.Read(await response.Content.ReadAsStringAsync(stop));
        }
        // A run that can't be fetched plays nothing.
        catch (HttpRequestException)
        {
            return [];
        }
    }

    /// <summary>Stops the play when the page goes away.</summary>
    public void Dispose()
    {
        _stop?.Cancel();
        _stop?.Dispose();
        GC.SuppressFinalize(this);
    }
}
