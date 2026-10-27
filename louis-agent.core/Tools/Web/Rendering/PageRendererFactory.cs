using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Rendering.Providers;
using LouisAgent.Core.Tools.Web.Rendering.Setups;
using LouisAgent.Choices;

namespace LouisAgent.Core.Tools.Web.Rendering;

/// <summary>
/// Picks the page renderer you configured, or else the first one that's set up: our own Chrome, then Tavily's Extract. A missing setting never stops start-up; a call just tells the model what to set.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. The renderers are a list of setups instead of a switch on the name, so adding a renderer is one new class.
/// A renderer that isn't set up gives one that explains what's missing, so nothing has to check for null.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public static class PageRendererFactory
{
    /// <summary>The renderer names you may put in `PAGE_RENDERER`, for the error message.</summary>
    public static IReadOnlyList<string> SupportedRenderers { get; } = SettingChoice.Names<PageRendererKind>();

    /// <summary>The renderers, in the order we fall back to them.</summary>
    internal static IReadOnlyList<IPageRendererSetup> Setups { get; } = [new ChromeSetup(), new TavilyRenderSetup()];

    /// <summary>Tells you which renderer these settings pick: the one named, or else the first that's set up. Nothing if the name is unknown or none is set up.</summary>
    public static PageRendererKind? Resolve(AgentOptions options) => Pick(options)?.Kind;

    /// <summary>Whether the renderer these settings pick is actually set up.</summary>
    public static bool IsReady(AgentOptions options) => Pick(options)?.IsReady(options) ?? false;

    /// <summary>Finds the setup of the renderer you named, or else the first one that's set up.</summary>
    private static IPageRendererSetup? Pick(AgentOptions options)
    {
        // A name we don't know picks nothing.
        if (!SettingChoice.TryParse(options.PageRenderer, out PageRendererKind? named)) return null;

        // A name we know gets that renderer, set up or not. No name gets the first that is set up, and nothing if none is.
        return named is { } kind ? Setups.Single(s => s.Kind == kind) : Setups.FirstOrDefault(s => s.IsReady(options));
    }

    /// <summary>Gives you the renderer these settings pick. If it isn't set up, the name is unknown, or nothing is set, you get one that tells the model what to do.</summary>
    /// <param name="browser">The process's one guarded browser, so a recorded run holds every page the shops read too. Chrome builds its own only if none is given.</param>
    public static IPageRenderer Create(AgentOptions options, HttpClient? http = null, IWebBrowser? browser = null)
    {
        // A name we don't know: say what was given and which names we do support.
        if (!SettingChoice.TryParse(options.PageRenderer, out PageRendererKind? _))
        {
            string named = options.PageRenderer!.Trim();
            return new UnavailablePageRenderer(MessageText.Format(WebToolMessage.UnknownPageRenderer,
                new { Renderer = named, Supported = string.Join(", ", SupportedRenderers) }));
        }

        // The picked renderer builds itself, or its "not set up" stand-in. With none picked at all, say what to set.
        return Pick(options) is { } setup
            ? setup.Create(options, http, browser)
            : new UnavailablePageRenderer(MessageText.Format(WebToolMessage.RenderNotConfigured));
    }
}
