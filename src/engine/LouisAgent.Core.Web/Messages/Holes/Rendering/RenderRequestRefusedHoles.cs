namespace LouisAgent.Core.Web.Messages.Holes.Rendering;

/// <summary>The request a page made to an address that isn't public, and the page.</summary>
public sealed record RenderRequestRefusedHoles(string Request, string Page);
