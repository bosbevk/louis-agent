using LouisAgent.Api.Evals.List;
using LouisAgent.Choices;
using LouisAgent.Core.Tools.Web.Rendering;
using LouisAgent.Core.Tools.Web.Search;

namespace LouisAgent.Api.Evals.Runs.Start;

/// <summary>
/// Writes down what a run ran with, so a later run can be compared with it knowing what changed: the model and roles,
/// the eval and its runner, the search engine and page renderer, how the web was handled, and which prompts were
/// used. There's no commit yet: the container has the workspace but no reason to run git for it.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public static class EvalSettings
{
    /// <summary>
    /// Collects what a run ran with: the model, the roles, the eval and its runner, and which search engine and page
    /// renderer it used and whether each was set up.
    /// </summary>
    /// <param name="searchProvider">The search engine this instance uses; left out if its name is unknown.</param>
    /// <param name="pageRenderer">The page renderer it uses; left out if none is set up.</param>
    /// <param name="searchProviderReady">
    /// Whether the search engine was set up. Only written when there is an engine.
    /// </param>
    /// <param name="pageRendererReady">
    /// Whether the page renderer was set up. Only written when there is a renderer.
    /// </param>
    /// <param name="evalRunner">
    /// The runner's path the API starts an eval from. Required, so a caller can't leave it out.
    /// </param>
    public static IReadOnlyDictionary<string, string> Of(EvalEntry check, string provider, string model,
        IReadOnlyList<string> roles, string evalRunner,
        WebSearchProviderKind? searchProvider, PageRendererKind? pageRenderer, bool searchProviderReady,
        bool pageRendererReady)
    {
        var settings = new Dictionary<string, string>
        {
            ["provider"] = provider,
            ["model"] = model,
            // An empty list means every role is on, so it is written as "(all)". Runs from before roles were called
            // roles kept this as personalities_enabled.
            ["roles_enabled"] = roles.Count == 0 ? "(all)" : string.Join(",", roles),
        };
        settings["eval"] = check.Id;
        settings["eval_runner"] = evalRunner;

        // The search engine and renderer the instance resolved to, so a run on SearXNG and one on Tavily can be told
        // apart. Beside each name goes whether it was set up: a named tool without its settings was only a stand-in,
        // and the run should say so. With no engine or renderer there is nothing to write, so the keys are left out
        // rather than written empty.
        if (searchProvider is { } search)
        {
            settings["web_search_provider"] = search.Wire();
            settings["web_search_provider_ready"] = searchProviderReady ? "true" : "false";
        }

        if (pageRenderer is { } renderer)
        {
            settings["page_renderer"] = renderer.Wire();
            settings["page_renderer_ready"] = pageRendererReady ? "true" : "false";
        }

        return settings;
    }

    /// <summary>
    /// Adds how the run's web was handled: whether it asked for one, and whether it replays an earlier run. The trend
    /// leaves replays out, since a replay costs nothing and measures nothing new.
    /// </summary>
    public static IReadOnlyDictionary<string, string> WithOptions(IReadOnlyDictionary<string, string> settings,
        EvalRunOptions options)
    {
        // Nothing asked for: the settings stay exactly as they were.
        if (options.Web is null && options.Replay is null) return settings;

        // Copy them, then add how the web was handled.
        var withOptions = new Dictionary<string, string>(settings);
        if (options.Web is { } web) withOptions["web"] = web.Wire();
        if (options.Replay is { } replayOf) withOptions["replay_of"] = replayOf;
        return withOptions;
    }

    /// <summary>
    /// Adds the hashes of the prompts the run used. It's the set the run used, not all it has, so a shorter list may
    /// only mean a different path through the eval and not a changed prompt.
    /// </summary>
    public static IReadOnlyDictionary<string, string>? WithPromptHashes(IReadOnlyDictionary<string, string>? settings,
        IReadOnlyList<string> hashes) =>
        hashes.Count == 0
            ? settings
            : new Dictionary<string, string>(settings ?? new Dictionary<string, string>())
            {
                ["prompt_hashes"] = string.Join(",", hashes)
            };
}
