namespace LouisAgent.Core.Tools.Web.Browsing;

/// <summary>A cookie a site keeps a visitor's choice in (F34: a shop's store for an area), as a visit starts with it or ends with it.</summary>
public sealed record BrowserCookie(string Name, string Value, string Domain);
