namespace LouisAgent.Web.Components.Widget.Orb;

/// <summary>
/// What the agent is doing, as its orb shows it (F134.3, the J and K mockups), with listening added from the F151
/// mockups (Louis's pick A of three: a fifth state, the other four as they were).
/// </summary>
public enum OrbState
{
    /// <summary>Waiting for the visitor: it breathes.</summary>
    Idle,

    /// <summary>The visitor is writing, or holds a piece of the page: a ring in their colour.</summary>
    Listening,

    /// <summary>Working before it says anything: an arc goes round.</summary>
    Thinking,

    /// <summary>Showing a place on the page: rings go out to it.</summary>
    Showing,

    /// <summary>Its words are arriving: it pulses with them.</summary>
    Speaking
}
