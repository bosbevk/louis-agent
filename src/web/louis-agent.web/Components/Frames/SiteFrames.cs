namespace LouisAgent.Web.Components.Frames;

/// <summary>The site's own layouts, for the framework's pages to draw inside. A site sets this once.</summary>
/// <param name="Admin">The layout the admin pages draw inside.</param>
/// <param name="Account">The layout the account pages (sign in, register, the profile) draw inside (F61.2).</param>
public sealed record SiteFrames(Type Admin, Type Account);
