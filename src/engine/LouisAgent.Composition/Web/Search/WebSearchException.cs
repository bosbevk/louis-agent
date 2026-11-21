
using System.Net;

namespace LouisAgent.Composition.Web.Search;
/// <summary>A search failure with an actionable message for the user.</summary>
public sealed class WebSearchException(string message) : Exception(message);
