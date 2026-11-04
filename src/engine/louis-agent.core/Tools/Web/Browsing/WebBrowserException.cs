namespace LouisAgent.Core.Tools.Web.Browsing;

/// <summary>A visit that couldn't be made, with a message the model can act on (the browser down, the page refused).</summary>
public sealed class WebBrowserException(string message) : Exception(message);
