
using LouisAgent.Composition.Web.Search;
using LouisAgent.Core.Web.Search;

namespace LouisAgent.Core.Web.Search.Providers;
/// <summary>What SearXNG answered (F32): its results, and the engines it asked that didn't answer, each with its reason.</summary>
internal sealed record SearxngAnswer(IReadOnlyList<WebSearchResult> Results, IReadOnlyList<string> SilentEngines);
