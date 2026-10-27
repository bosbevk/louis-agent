using LouisAgent.Core.Config;

namespace LouisAgent.Core.Tools.Web.Search.Setups;

/// <summary>
/// One of these per search engine. It knows whether the engine is set up and how to build it. If it isn't set up you get a provider that tells the model what to set, not an error.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. "Is it ready?" and "build it" live in the same class, so they can't drift apart, and a new engine is one new class instead of another case in two switches.
/// A setup that isn't ready hands back a provider that explains what's missing, so nothing has to check for null and the model still gets a useful answer.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public interface ISearchProviderSetup
{
    /// <summary>Which engine this is.</summary>
    WebSearchProviderKind Kind { get; }

    /// <summary>We can only use the engine if its settings are there.</summary>
    bool IsReady(AgentOptions options);

    /// <summary>Builds the provider. If the engine isn't set up, you get one that tells the model what to set.</summary>
    IWebSearchProvider Create(AgentOptions options, HttpClient? http);
}
