namespace LouisAgent.Web.Components.Admin;

/// <summary>The site's own layouts, for the framework's pages to draw inside. A site sets this once.</summary>
/// <param name="Admin">The layout the admin pages draw inside.</param>
public sealed record SiteFrames(Type Admin);
