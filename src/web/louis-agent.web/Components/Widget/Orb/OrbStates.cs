namespace LouisAgent.Web.Components.Widget.Orb;

/// <summary>
/// Which state the orb is in, read from the chat's own state (F134.3): a place just shown is showing, whatever else;
/// streaming with no words yet is thinking; words arriving is speaking; and the rest is idle.
/// </summary>
/// <remarks>
/// A switch on the shape of the three facts, read like a table (ADR 0051). A presentation rule kept out of the
/// component, so it is tested with no browser, as the deck's `DeckPlace` is. It
/// reads only what the chat says of itself, so the orb never shows a state the agent isn't in.
/// </remarks>
public static class OrbStates
{
    /// <summary>The orb's state for what the chat says it is doing.</summary>
    public static OrbState Of(bool streaming, bool hasWords, bool showing) => (streaming, hasWords, showing) switch
    {
        (_, _, true) => OrbState.Showing,
        (true, false, _) => OrbState.Thinking,
        (true, true, _) => OrbState.Speaking,
        _ => OrbState.Idle
    };
}
