using LouisAgent.Api.Contracts.Admin.Screens;

namespace LouisAgent.Web.Components.Packs;

/// <summary>A component that can draw a screen: the page hands it the description, and the rest is up to it.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public interface IScreenView
{
    /// <summary>The screen to draw, as its pack described it.</summary>
    ScreenReply Screen { get; set; }
}
