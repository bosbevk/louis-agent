using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;
using LouisAgent.Core.Tools.Web.Browsing;
using LouisAgent.Core.Tools.Web.Messages;
using LouisAgent.Core.Tools.Web.Rendering.Providers;

namespace LouisAgent.Core.Tools.Web.Rendering.Setups;

/// <summary>Strategy: Tavily's Extract as a renderer. Ready with <c>TAVILY_API_KEY</c> (the same key as its search); otherwise <see cref="Create"/> returns its Null Object, an unavailable renderer that says what to set.</summary>
internal sealed class TavilyRenderSetup : IPageRendererSetup
{
    /// <summary>Tavily.</summary>
    public PageRendererKind Kind => PageRendererKind.Tavily;

    /// <summary>True when the Tavily API key is set and not blank.</summary>
    public bool IsReady(AgentOptions options) => !string.IsNullOrWhiteSpace(options.TavilyApiKey);

    /// <summary>The Tavily renderer when ready; otherwise the Null Object that says what is missing.</summary>
    public IPageRenderer Create(AgentOptions options, HttpClient? http, IWebBrowser? browser) =>
        IsReady(options)
            ? new TavilyPageRenderer(options.TavilyApiKey!, http)
            : new UnavailablePageRenderer(MessageText.Format(WebToolMessage.RenderNotConfigured));
}
