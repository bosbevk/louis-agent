namespace LouisAgent.Core.Tools.Web;

using System.Net;

/// <summary>A search request: <see cref="Recency"/> is null or one of day, week, month, year.</summary>
public sealed record WebSearchQuery(string Text, int MaxResults, string? Site = null, string? Recency = null);
