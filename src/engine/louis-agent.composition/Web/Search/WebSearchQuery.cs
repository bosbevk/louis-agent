
using System.Net;

namespace LouisAgent.Composition.Web.Search;
/// <summary>A search request: <see cref="Recency"/> is null for any time.</summary>
public sealed record WebSearchQuery(string Text, int MaxResults, string? Site = null, SearchRecency? Recency = null);
