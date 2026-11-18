namespace LouisAgent.Web.Components.Recorded;

/// <summary>
/// When a recorded chat starts playing (F160): as soon as it is on the page, or only once someone asks, the visitor's
/// Play button or a guide naming a turn.
/// </summary>
public enum RecordedStart
{
    /// <summary>It plays as soon as it is on the page.</summary>
    OnItsOwn,

    /// <summary>Its first question waits with a Play button, and nothing plays until it is asked to.</summary>
    WhenAsked,
}
