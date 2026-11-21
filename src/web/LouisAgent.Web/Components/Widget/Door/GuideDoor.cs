namespace LouisAgent.Web.Components.Widget.Door;

/// <summary>
/// The way into the site's chat from anywhere on a site (F118): a button puts a question through it, and the chat
/// widget, listening, opens and sends it, so a page offers "take the tour" without knowing the widget.
/// </summary>
/// <remarks>
/// Observer: one small shared object every site gets with the chat's plumbing (F164.2); the widget listens, any number
/// of buttons ask. Without it a page would need a reference to the widget in its layout, which it can't reach.
/// </remarks>
public sealed class GuideDoor
{
    /// <summary>Raised with the question a button asked, or with none when a button only opens the chat.</summary>
    public event Action<string?>? Asked;

    /// <summary>Asks the guide a question.</summary>
    public void Ask(string question) => Asked?.Invoke(question);

    /// <summary>
    /// Opens the chat and asks nothing (F164.2), as a header's "Ask Orbi" does, so the visitor types the first words.
    /// </summary>
    public void Open() => Asked?.Invoke(null);
}
