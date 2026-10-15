namespace LouisAgent.Core.Tools.Web;

/// <summary>A renderer that isn't configured: every call explains what to set, so start-up never fails over it.</summary>
public sealed class UnavailablePageRenderer(string reason) : IPageRenderer
{
    public string Name => "none";

    public Task<RenderedPage> RenderAsync(Uri url, CancellationToken cancellationToken = default) => throw new PageRenderException(reason);
}
