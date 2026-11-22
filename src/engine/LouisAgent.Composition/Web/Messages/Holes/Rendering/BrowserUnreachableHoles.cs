namespace LouisAgent.Composition.Web.Messages.Holes.Rendering;

/// <summary>Where the browser should be, and why it didn't answer.</summary>
public sealed record BrowserUnreachableHoles(string Url, string Reason);
