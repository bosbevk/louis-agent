using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;

namespace LouisAgent.Api.Site.Rendering;

/// <summary>
/// What the prerenderer renders (F121): a head outlet and the page, each in a marked block, so the page's title and
/// head come out of the same render as its body and <see cref="PrerenderedHtml"/> can take them apart.
/// </summary>
internal sealed class PrerenderRoot : ComponentBase
{
    /// <summary>The marker before the head's HTML.</summary>
    internal const string HeadMark = "<div data-prerender-head>";

    /// <summary>The marker before the page's HTML.</summary>
    internal const string BodyMark = "<div data-prerender-body>";

    /// <summary>The site's page to render.</summary>
    [Parameter] public Type PageType { get; set; } = null!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "data-prerender-head");
        builder.OpenComponent<HeadOutlet>(2);
        builder.CloseComponent();
        builder.CloseElement();
        builder.OpenElement(3, "div");
        builder.AddAttribute(4, "data-prerender-body");
        builder.OpenComponent<DynamicComponent>(5);
        builder.AddComponentParameter(6, nameof(DynamicComponent.Type), PageType);
        builder.CloseComponent();
        builder.CloseElement();
    }
}
