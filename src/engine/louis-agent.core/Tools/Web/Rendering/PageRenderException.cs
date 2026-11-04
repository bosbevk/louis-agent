namespace LouisAgent.Core.Tools.Web.Rendering;

/// <summary>A rendering problem the model should see as is: no key, a page the renderer couldn't open, a limit.</summary>
public sealed class PageRenderException(string message) : Exception(message);
