using LouisAgent.Core.Config;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>
/// Strategy: one small class per search engine that knows whether the engine's settings are there (<see cref="IsReady"/>) and how to build its provider
/// (<see cref="Create"/>), so "ready" and "build" live together and cannot drift. <see cref="Create"/> always returns a working provider, or the setup's own
/// Null Object (an <c>UnavailableSearchProvider</c> that says what to set) when the settings are missing; the factory never checks readiness itself.
/// </summary>
public interface ISearchProviderSetup
{
    /// <summary>Which engine this setup is for.</summary>
    WebSearchProviderKind Kind { get; }

    /// <summary>Whether the settings this engine needs are present in <paramref name="options"/>.</summary>
    bool IsReady(AgentOptions options);

    /// <summary>The engine's provider when it is ready; otherwise its Null Object, an unavailable provider whose every search says what is missing.</summary>
    IWebSearchProvider Create(AgentOptions options, HttpClient? http);
}
