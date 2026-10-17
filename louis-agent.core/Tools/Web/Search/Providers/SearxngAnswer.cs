namespace LouisAgent.Core.Tools.Web.Search.Providers;

using LouisAgent.Core.Tools.Web.Search;

/// <summary>What SearXNG answered (F32): its results, and the engines it asked that didn't answer, each with its reason.</summary>
internal sealed record SearxngAnswer(IReadOnlyList<WebSearchResult> Results, IReadOnlyList<string> SilentEngines);
