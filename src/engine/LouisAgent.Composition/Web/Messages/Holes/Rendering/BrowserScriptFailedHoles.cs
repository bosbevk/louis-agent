namespace LouisAgent.Composition.Web.Messages.Holes.Rendering;

/// <summary>The page whose script the browser couldn't run, and why.</summary>
public sealed record BrowserScriptFailedHoles(string Url, string Reason);
