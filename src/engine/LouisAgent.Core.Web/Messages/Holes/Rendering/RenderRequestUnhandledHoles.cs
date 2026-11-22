namespace LouisAgent.Core.Web.Messages.Holes.Rendering;

/// <summary>The request that couldn't be answered while rendering, the page, and why.</summary>
public sealed record RenderRequestUnhandledHoles(string Request, string Page, string Reason);
