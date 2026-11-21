namespace LouisAgent.Web.Components.Staging.Windows;

/// <summary>
/// The one thing an agent showed in a window's chat, such as a room or a dish: its name, a few words under it, and
/// its photo if it has one. The window draws it as a small card under the agent's answer.
/// </summary>
/// <remarks>
/// A record for a value (ADR 0040). The site hands the window the card's words and the window draws them, so a site
/// writes no markup of its own for it (ADR 0125).
/// </remarks>
/// <param name="Name">What it is, in bold.</param>
/// <param name="Note">A few words under the name, such as its price.</param>
/// <param name="Photo">Its photo, if it has one.</param>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public sealed record ShownCard(string Name, string? Note = null, string? Photo = null);
