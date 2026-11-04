namespace LouisAgent.Core.Tools.Web.Search;

using System.Net;

/// <summary>A search failure with an actionable message for the user.</summary>
public sealed class WebSearchException(string message) : Exception(message);
