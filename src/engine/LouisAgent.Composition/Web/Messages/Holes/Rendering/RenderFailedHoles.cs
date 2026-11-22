namespace LouisAgent.Composition.Web.Messages.Holes.Rendering;

/// <summary>The page that couldn't be rendered, and why.</summary>
public sealed record RenderFailedHoles(string Url, string Reason);
