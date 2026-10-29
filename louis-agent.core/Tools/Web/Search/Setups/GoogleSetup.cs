using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>
/// Searches through Google's custom search. It needs both the API key and the search engine ID; one without the other is no use.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. Google only works with both settings, so only this class can say whether it's ready, and when one is missing the search says which.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
internal sealed class GoogleSetup : ISearchProviderSetup
{
    /// <summary>This is the Google one.</summary>
    public WebSearchProviderKind Kind => WebSearchProviderKind.Google;

    /// <summary>We need both the key and the engine ID.</summary>
    public bool IsReady(AgentOptions options) =>
        !string.IsNullOrWhiteSpace(options.GoogleSearchApiKey) &&
        !string.IsNullOrWhiteSpace(options.GoogleSearchEngineId);

    /// <summary>Builds the Google provider, or the one that says what is missing.</summary>
    public IWebSearchProvider Create(AgentOptions options, HttpClient? http) =>
        IsReady(options)
            ? new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http)
            : new UnavailableSearchProvider("Google", MessageText.Format(WebToolMessage.GoogleNotConfigured));
}
