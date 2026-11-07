using LouisAgent.Choices;

namespace LouisAgent.Composition.Packs.Screens;

/// <summary>
/// The kinds of value a column can hold: the whole vocabulary. A new kind enters only when a second screen needs it;
/// until then a screen that wants more uses a real component.
/// </summary>
public enum ScreenCell
{
    [WireName("text")] Text,
    [WireName("number")] Number,
    [WireName("link")] Link,
    [WireName("date")] Date,
}
