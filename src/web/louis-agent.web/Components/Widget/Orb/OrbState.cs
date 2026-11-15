namespace LouisAgent.Web.Components.Widget.Orb;

/// <summary>What the agent is doing, as its orb shows it (F134.3, the J and K mockups).</summary>
public enum OrbState
{
    /// <summary>Waiting for the visitor: it breathes.</summary>
    Idle,

    /// <summary>Working before it says anything: an arc goes round.</summary>
    Thinking,

    /// <summary>Showing a place on the page: rings go out to it.</summary>
    Showing,

    /// <summary>Its words are arriving: it pulses with them.</summary>
    Speaking
}
