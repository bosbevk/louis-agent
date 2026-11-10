namespace LouisAgent.Web.Components.Widget;

/// <summary>
/// How big a chat window sits (F127.2, Louis: "we should have an minimize and maximize"): as it comes, folded to its
/// head, or spread wide to read the conversation. The guide's widget and a mock website's chat both have it.
/// </summary>
public enum ChatWindowSize
{
    /// <summary>As it comes, in its corner.</summary>
    Normal,

    /// <summary>Folded to its head, so the page under it shows.</summary>
    Minimised,

    /// <summary>Spread wide, to read the conversation.</summary>
    Maximised,
}
