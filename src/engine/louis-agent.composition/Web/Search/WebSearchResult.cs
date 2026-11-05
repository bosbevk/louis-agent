namespace LouisAgent.Composition.Web.Search;

/// <summary>One hit a web search gave back: its title, its address and the snippet the engine showed under it.</summary>
public sealed record WebSearchResult(string Title, string Url, string Snippet);
