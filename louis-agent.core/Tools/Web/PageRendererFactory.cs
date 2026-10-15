namespace LouisAgent.Core.Tools.Web;

using LouisAgent.Core.Config;
using LouisAgent.Core.Messages;

/// <summary>
/// Picks the page renderer from configuration: Tavily's Extract when TAVILY_API_KEY is set (the same key as its
/// search), otherwise a renderer whose every call says what to set. Misconfiguration never fails start-up.
/// </summary>
public static class PageRendererFactory
{
    public static IPageRenderer Create(AgentOptions options, HttpClient? http = null) =>
        string.IsNullOrWhiteSpace(options.TavilyApiKey)
            ? new UnavailablePageRenderer(MessageText.Format(ToolMessage.RenderNotConfigured))
            : new TavilyPageRenderer(options.TavilyApiKey, http);
}
