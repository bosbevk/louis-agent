namespace LouisAgent.Core.Tools.Web;

/// <summary>
/// Renders a page the way a browser would and returns its text (F25 step 7): for sites that draw their content with
/// JavaScript, which <see cref="WebTools.FetchUrl"/> reads as empty. One implementation per renderer (Tavily's
/// Extract today), picked in <see cref="PageRendererFactory"/>.
/// </summary>
public interface IPageRenderer
{
    /// <summary>Shown to the model, e.g. "Tavily".</summary>
    string Name { get; }

    /// <summary>
    /// The page's readable text. Throw <see cref="PageRenderException"/> for problems the user can act on (no key, a
    /// page the renderer couldn't open, a plan's limit); its message is shown as is.
    /// </summary>
    Task<RenderedPage> RenderAsync(Uri url, CancellationToken cancellationToken = default);
}
