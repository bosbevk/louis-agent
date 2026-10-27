namespace LouisAgent.Core.Tools.Web.Rendering;

using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Rendering.Providers;
using LouisAgent.Core.Tools.Web.Rendering.Setups;
using LouisAgent.Choices;

/// <summary>
/// Picks the page renderer from configuration: PAGE_RENDERER = chrome | tavily (F32; an enum, ADR 0024). When unset, the
/// headless Chrome we run is used if BROWSER_URL is set, else Tavily's Extract if TAVILY_API_KEY is (the same key as its
/// search), otherwise a renderer whose every call says what to set. Misconfiguration never fails start-up. Strategy + Null Object (rule 11): each renderer is a
/// setup that knows if it is ready and how to build itself, and a setup that is not ready hands back its unavailable renderer, so the factory only picks.
/// </summary>
public static class PageRendererFactory
{
    /// <summary>The names <c>PAGE_RENDERER</c> takes, from <see cref="PageRendererKind"/>.</summary>
    public static IReadOnlyList<string> SupportedRenderers { get; } = SettingChoice.Names<PageRendererKind>();

    /// <summary>Strategy: one setup per renderer, in fallback order (Chrome, Tavily). With no renderer named, the first ready setup is the one used.</summary>
    internal static IReadOnlyList<IPageRendererSetup> Setups { get; } = [new ChromeSetup(), new TavilyRenderSetup()];

    /// <summary>
    /// The renderer <paramref name="options"/> select: the one named, else the first ready one; null when the name is unknown or nothing is set. Named and not ready
    /// still gives that renderer, whose <see cref="Create"/> is then its Null Object.
    /// </summary>
    public static PageRendererKind? Resolve(AgentOptions options) => Pick(options)?.Kind;

    /// <summary>Strategy: the setup of the renderer named, else the first ready one in <see cref="Setups"/>; null when the name is unknown or none is ready.</summary>
    private static IPageRendererSetup? Pick(AgentOptions options)
    {
        if (!SettingChoice.TryParse(options.PageRenderer, out PageRendererKind? named)) return null;
        return named is { } kind ? Setups.Single(s => s.Kind == kind) : Setups.FirstOrDefault(s => s.IsReady(options));
    }

    /// <summary>
    /// The page renderer for <paramref name="options"/>: the picked setup's, which is its Null Object when its settings are missing; an unknown name gets the unavailable renderer
    /// that lists the supported names, and nothing set gets the one that says what to set.
    /// </summary>
    /// <param name="browser">The process's one guarded browser (F18 step 4a: the shops read through it too, so a recorded run holds every page); the Chrome renderer builds its own only when none is given.</param>
    public static IPageRenderer Create(AgentOptions options, HttpClient? http = null, IWebBrowser? browser = null)
    {
        if (!SettingChoice.TryParse(options.PageRenderer, out PageRendererKind? _))
        {
            string named = options.PageRenderer!.Trim();
            return new UnavailablePageRenderer(MessageText.Format(WebToolMessage.UnknownPageRenderer,
                new { Renderer = named, Supported = string.Join(", ", SupportedRenderers) }));
        }

        return Pick(options) is { } setup
            ? setup.Create(options, http, browser)
            : new UnavailablePageRenderer(MessageText.Format(WebToolMessage.RenderNotConfigured));
    }
}
