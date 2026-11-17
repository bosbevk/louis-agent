using System.Text.Json.Serialization;
using LouisAgent.Diagrams.Icons;
using LouisAgent.Diagrams.Tones;

namespace LouisAgent.Diagrams.Cards;

/// <summary>
/// One card of an overview: what a link names it by, its icon, its title, a project or type in a code chip beside
/// the title when it has one, a line saying what it is, how it looks, plain unless it says otherwise, and the tone
/// its badge, tint and icon wear, the picture's violet unless it names one (F160.1).
/// </summary>
public sealed record Card(
    string Id,
    // Written always, though chip is the first icon: a card's file names its icon.
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    DiagramIcon Icon,
    string Title,
    string? Code,
    string Text,
    CardLook Look = CardLook.Plain,
    DiagramTone? Tone = null);
