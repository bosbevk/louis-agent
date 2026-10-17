namespace LouisAgent.Core.Tools.Web.Rendering;

/// <summary>
/// The page renderers <c>PAGE_RENDERER</c> can name (F32 step 2; ADR 0024), in the order the "supported" message lists
/// them. Parsed once in <see cref="PageRendererFactory"/>.
/// </summary>
public enum PageRendererKind
{
    Chrome,
    Tavily,
}
