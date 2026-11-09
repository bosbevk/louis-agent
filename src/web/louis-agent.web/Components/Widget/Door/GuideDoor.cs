namespace LouisAgent.Web.Components.Widget.Door;

/// <summary>
/// The way into the guide in the corner from anywhere on a site (F118): a button puts a question through it, and the
/// chat widget, listening, opens and sends it, so a page offers "take the tour" without knowing the widget.
/// </summary>
/// <remarks>
/// Observer: one small shared object the site registers once; the widget listens, any number of buttons ask. Without
/// it a page would need a reference to the widget in its layout's corner, which it can't reach.
/// </remarks>
public sealed class GuideDoor
{
    /// <summary>Raised with the question a button asked.</summary>
    public event Action<string>? Asked;

    /// <summary>Asks the guide a question.</summary>
    public void Ask(string question) => Asked?.Invoke(question);
}
