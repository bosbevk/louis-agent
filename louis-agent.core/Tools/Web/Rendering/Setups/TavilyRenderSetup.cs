using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.Tools.Web.Rendering.Setups;

/// <summary>
/// Reads pages through Tavily's Extract, the paid fallback. It uses the same key as Tavily search, so one key covers both.
/// </summary>
/// <remarks>
/// Strategy, with a Null Object. The key that covers search covers rendering too, so the sold endpoint needs no extra setting; without it a call says what to set.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0023-copyleft-service-beside-us.md">ADR 0023: a copyleft service may run beside us</seealso>
internal sealed class TavilyRenderSetup : IPageRendererSetup
{
    /// <summary>This is the Tavily one.</summary>
    public PageRendererKind Kind => PageRendererKind.Tavily;

    /// <summary>We need the Tavily key.</summary>
    public bool IsReady(AgentOptions options) => !string.IsNullOrWhiteSpace(options.TavilyApiKey);

    /// <summary>Builds the Tavily renderer, or the one that says what to set.</summary>
    public IPageRenderer Create(AgentOptions options, HttpClient? http, IWebBrowser? browser) =>
        IsReady(options)
            ? new TavilyPageRenderer(options.TavilyApiKey!, http)
            : new UnavailablePageRenderer(MessageText.Format(WebToolMessage.RenderNotConfigured));
}
