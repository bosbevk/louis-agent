using LouisAgent.Web.Api;
using LouisAgent.Web.Storage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Admin.Reset;

/// <summary>
/// The admin's Reset my session (F153): a line saying what it does and one press, which hands the work to
/// <see cref="SessionReset"/> and says it is resetting while it runs.
/// </summary>
/// <remarks>
/// It only draws and presses, Louis's pick A of three: the reset is its own class, so its order is tested without a
/// page. One press resets with no second press to confirm: what it clears is this browser's visitor state, and the
/// admin's key stays.
/// </remarks>
public partial class ResetSession
{
    [Inject] private IJSRuntime Js { get; set; } = null!;
    [Inject] private ChatSettings Settings { get; set; } = null!;
    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <summary>Whether a reset is running, so the press waits and says so.</summary>
    private bool _resetting;

    /// <summary>Resets this browser; the page reloads at the end, so nothing is drawn after.</summary>
    private async Task ResetAsync()
    {
        _resetting = true;
        var reset = new SessionReset(Js, Settings, Api, Navigation);
        await reset.RunAsync();
    }
}
