using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Search.Providers;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>Strategy: Google's setup. Ready with both the API key and the search engine ID; otherwise <see cref="Create"/> returns its Null Object, an unavailable provider that says what to set.</summary>
internal sealed class GoogleSetup : ISearchProviderSetup
{
    /// <summary>Google.</summary>
    public WebSearchProviderKind Kind => WebSearchProviderKind.Google;

    /// <summary>True when both the Google API key and the search engine ID are set and not blank.</summary>
    public bool IsReady(AgentOptions options) => !string.IsNullOrWhiteSpace(options.GoogleSearchApiKey) && !string.IsNullOrWhiteSpace(options.GoogleSearchEngineId);

    /// <summary>The Google provider when ready; otherwise the Null Object that says what is missing.</summary>
    public IWebSearchProvider Create(AgentOptions options, HttpClient? http) =>
        IsReady(options)
            ? new GoogleSearchProvider(options.GoogleSearchApiKey!, options.GoogleSearchEngineId!, http)
            : new UnavailableSearchProvider("Google", MessageText.Format(WebToolMessage.GoogleNotConfigured));
}
