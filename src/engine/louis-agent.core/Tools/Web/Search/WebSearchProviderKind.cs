namespace LouisAgent.Core.Tools.Web.Search;

/// <summary>
/// The search engines <c>WEB_SEARCH_PROVIDER</c> can name (F32 step 1b; ADR 0024), in the order the "supported" message
/// lists them; <c>ddg</c> is an alias of <see cref="DuckDuckGo"/>. Parsed once in <see cref="WebSearchProviderFactory"/>.
/// </summary>
public enum WebSearchProviderKind
{
    Tavily,
    Google,
    Searxng,
    DuckDuckGo,
}
