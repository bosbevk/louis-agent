namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// How big the agent's chat sits on its mock website (F127.2, Louis: "we should have an minimize and maximize"): in its
/// corner, folded to its head, or over the whole website.
/// </summary>
public enum MockWidgetSize
{
    /// <summary>In its corner, as it comes.</summary>
    Normal,

    /// <summary>Folded to its head, so the whole website shows.</summary>
    Minimised,

    /// <summary>Over the whole website, to read the conversation.</summary>
    Maximised,
}
