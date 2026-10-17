namespace LouisAgent.Core.Tools.Web.Browsing;

/// <summary>A page as the browser left it: its address after any redirect, its HTML, and the response bodies captured.</summary>
public sealed record BrowserVisit(string Url, string Html, IReadOnlyList<string> Captured);
