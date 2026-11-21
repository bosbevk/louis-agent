using LouisAgent.Choices;

namespace LouisAgent.Composition.Packs.Screens.Acting;

/// <summary>Something a screen can do when asked, as a button: what it's called, what it says, and what it acts on.</summary>
/// <param name="Name">"remove": how the button names it, unique on its screen.</param>
/// <param name="Label">What the button says.</param>
/// <param name="On">Whether it needs a row, or acts on the whole screen.</param>
/// <param name="Confirm">
/// The question a destructive action asks first, which takes a second click; with none, one click does it.
/// </param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public sealed record ScreenAction(string Name, string Label, ScreenActionOn On, string? Confirm = null);

/// <summary>What an action acts on.</summary>
public enum ScreenActionOn
{
    /// <summary>One row, so a button sits on each.</summary>
    [WireName("row")] Row,

    /// <summary>The whole screen, so one button sits above the rows.</summary>
    [WireName("screen")] Screen
}
