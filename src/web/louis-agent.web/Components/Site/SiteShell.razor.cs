using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Site;

/// <summary>
/// A site's public frame: a header with the brand, the menu and the actions, the page, a footer, and a corner that
/// floats over every page, with slots for what's a site's own.
/// </summary>
/// <remarks>
/// Template Method, in Razor (Gang of Four), as <see cref="Admin.AdminShell"/> is: the frame stays here, and a site's
/// small layout fills in the steps that differ, as ordinary slots the compiler checks. The corner is a slot and not a
/// chat of its own, so the frame knows nothing about what floats there: a site puts its chat widget in, or nothing.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0084-every-site-is-built-on-the-frameworks-web.md">ADR 0084: every site is built on the framework's web</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/template-method">Template Method (Refactoring.Guru)</seealso>
public partial class SiteShell
{
    /// <summary>The site's name or logo, at the start of the header.</summary>
    [Parameter]
    public RenderFragment? Brand { get; set; }

    /// <summary>The site's slogan, one short line under the brand (FX79); left empty, the brand stands alone.</summary>
    [Parameter]
    public RenderFragment? Tagline { get; set; }

    /// <summary>Where the brand takes you; home unless the site says otherwise.</summary>
    [Parameter]
    public string BrandHref { get; set; } = "/";

    /// <summary>The menu's links.</summary>
    [Parameter]
    public RenderFragment? Links { get; set; }

    /// <summary>What comes after the menu, such as the way into the chat.</summary>
    [Parameter]
    public RenderFragment? Actions { get; set; }

    /// <summary>The footer's content.</summary>
    [Parameter]
    public RenderFragment? Footer { get; set; }

    /// <summary>What floats in the lower-right corner of every page, such as the site's chat widget.</summary>
    [Parameter]
    public RenderFragment? Corner { get; set; }

    /// <summary>The page.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private JsModule? _motion;

    /// <summary>The frame's own module (F110): the progress line, the compact header, the index, the reveals.</summary>
    private JsModule Motion => _motion ??= new JsModule(JS, FrameworkScripts.SiteShell);

    /// <summary>
    /// Attaches the motion after every render, not only the first: a page that just drew has reveals to observe, and
    /// the module hangs its listeners once.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender) => await Motion.InvokeVoidAsync("attach");

    /// <summary>Takes the listeners down and lets the module go; a closed browser tab has no JS runtime to tell.</summary>
    public async ValueTask DisposeAsync()
    {
        if (_motion is null) return;
        try
        {
            await _motion.InvokeVoidAsync("detach");
        }
        catch (JSDisconnectedException)
        {
        }

        await _motion.DisposeAsync();
    }
}
