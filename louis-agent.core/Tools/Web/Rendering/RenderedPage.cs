namespace LouisAgent.Core.Tools.Web.Rendering;

/// <summary>A rendered page: the URL as the renderer reports it, and its text (markdown-like).</summary>
public sealed record RenderedPage(string Url, string Text);
