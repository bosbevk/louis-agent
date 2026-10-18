namespace LouisAgent.Core.Tools.Web.Browsing;

/// <summary>
/// What a visit starts with (F34 step 1a): the cookies a site keeps a visitor's choice in, as Checkers keeps the store
/// that serves an area. A visit in a session has a browser context of its own, dropped after, so the cookies never reach
/// another visit; an empty session is that context with no cookies.
/// </summary>
public sealed record BrowserSession(IReadOnlyList<BrowserCookie> Cookies);
