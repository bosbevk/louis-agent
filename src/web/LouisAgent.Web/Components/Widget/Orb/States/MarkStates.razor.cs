using System.Globalization;

namespace LouisAgent.Web.Components.Widget.Orb.States;

/// <summary>
/// The mark at the top of the landing (F151.6, Louis's pick: the mockup's top): the framework's orb drawn large, the
/// name of the state it is in with a line on what that means, and a button for each state. It plays the states in
/// turn, and a press holds the one pressed, so a visitor can look at it as long as they like.
/// </summary>
/// <remarks>
/// The landing's own piece, since its words are this site's; the mark it draws is the framework's orb. What the
/// states are, and their pace, it asks <see cref="MarkSteps"/>.
/// </remarks>
public partial class MarkStates : IDisposable
{
    private int _at;
    private bool _held;
    private Timer? _turn;

    /// <summary>The state on show.</summary>
    private MarkStep Now => MarkSteps.All[_at];

    /// <summary>The row of buttons, and whether a visitor is holding a state.</summary>
    private string StepsClasses => _held ? "mark-steps is-held" : "mark-steps";

    /// <summary>The pace, handed to the styles, so the line under a button fills while its state is shown.</summary>
    private static string Pace =>
        $"--mark-pace: {MarkSteps.Pace.TotalSeconds.ToString(CultureInfo.InvariantCulture)}s";

    /// <summary>Stops the turns when the page goes.</summary>
    public void Dispose()
    {
        _turn?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>Once drawn, the states start to play in turn.</summary>
    protected override void OnAfterRender(bool firstRender)
    {
        if (!firstRender) return;

        _turn = new Timer(_ => InvokeAsync(Play), null, MarkSteps.Pace, MarkSteps.Pace);
    }

    /// <summary>Whether this button's state is the one on show, as the attribute wants it said.</summary>
    private string Current(int place) => place == _at ? "true" : "false";

    /// <summary>Shows the next state, unless a visitor holds one.</summary>
    private void Play()
    {
        if (_held) return;

        _at = MarkSteps.After(_at);
        StateHasChanged();
    }

    /// <summary>Shows the state a visitor pressed, and stops the turns.</summary>
    private void Hold(int place)
    {
        _at = place;
        _held = true;
        _turn?.Dispose();
    }
}
