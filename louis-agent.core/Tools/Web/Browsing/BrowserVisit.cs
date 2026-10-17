namespace LouisAgent.Core.Tools.Web.Browsing;

/// <summary>
/// A page as the browser left it: its address after any redirect, its HTML, the response bodies captured; and, from a
/// visit with a session or a script (F34), the script's result and the cookies the site set.
/// </summary>
public sealed record BrowserVisit(string Url, string Html, IReadOnlyList<string> Captured, string? ScriptResult = null,
    IReadOnlyList<BrowserCookie>? Cookies = null);
