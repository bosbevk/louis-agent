using LouisAgent.Web.Chats;

namespace LouisAgent.Web.Components.Widget.Door;

/// <summary>
/// What a guide in the corner is doing, kept apart from the widget that draws it (FX135): its runner, with the chat,
/// the session and an answer still streaming, whether its panel is open and how big, and the page its last message
/// came from. A widget the next layout builds takes it over, so the talk carries on.
/// </summary>
/// <remarks>
/// Memento (Gang of Four): the state outlives the component that holds it, since a move between two layouts throws
/// the widget away and builds another, even mid-answer. Louis's pick was one guide for the whole site; the frames place
/// their corners each their own way, so the state moves between them instead of the guide.
/// </remarks>
/// <seealso href="https://refactoring.guru/design-patterns/memento">Memento (Refactoring.Guru)</seealso>
public sealed class KeptGuide
{
    /// <summary>The runner of its talk; the first widget builds it, every later one takes it over.</summary>
    public ChatRunner? Runner { get; set; }

    /// <summary>Whether its panel is open.</summary>
    public bool Open { get; set; }

    /// <summary>How big its panel is.</summary>
    public ChatWindowSize Size { get; set; }

    /// <summary>The page its last message carried, so a new page's words go once.</summary>
    public string? PageSentFor { get; set; }
}
