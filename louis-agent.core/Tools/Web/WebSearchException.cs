namespace LouisAgent.Core.Tools.Web;

using System.Net;

/// <summary>A search failure with an actionable message for the user.</summary>
public sealed class WebSearchException(string message) : Exception(message);
