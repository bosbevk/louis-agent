using LouisAgent.Web.Components.Site.Addresses;
using LouisAgent.Web.Components.Site.Bands;
using LouisAgent.Web.Components.Site.Slides;
using LouisAgent.Web.Scripts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
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

    /// <summary>
    /// How the site's pages are laid out (F118): flowing, unless the site asks for slides; a page may choose the other
    /// for itself with a <see cref="PageLayoutChoice"/>.
    /// </summary>
    [Parameter]
    public PageLayout Layout { get; set; } = PageLayout.Flow;

    /// <summary>What the page in view asks of the frame: its own layout, when it has one.</summary>
    private readonly PageOptions _options = new();

    /// <summary>The page's choice, else the site's.</summary>
    private PageLayout Laid => _options.Layout ?? Layout;

    /// <summary>The frame's root classes: the frame, and the layout the page is drawn in, which its styles read.</summary>
    private string SiteClasses => Laid == PageLayout.Slides ? "site layout-slides" : "site layout-flow";

    /// <summary>Redraws when the page makes or takes back its choice.</summary>
    protected override void OnInitialized() => _options.Changed += Redraw;

    private void Redraw() => InvokeAsync(StateHasChanged);

    [Inject] private IJSRuntime JS { get; set; } = default!;

    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    /// <summary>The site's addresses (ADR 0107), when the site has them.</summary>
    private SiteAddresses? Addresses => Services.GetService<SiteAddresses>();

    /// <summary>Where the address points: the page, and the place on it when it names one (F118, ADR 0107).</summary>
    private SiteAddress Here =>
        Addresses?.Read(Nav.Uri) ?? new SiteAddress(Nav.ToAbsoluteUri(Nav.Uri).AbsolutePath);

    /// <summary>
    /// The page's own address, without the place on it (F118): the main column is keyed by it, so a new page draws a
    /// new column and its entrance plays, while a move within the page keeps the one there.
    /// </summary>
    private string PagePath => Here.Page;

    /// <summary>The page's areas (F118): the bands fill it as they draw, and the index on the left reads it.</summary>
    private readonly PageAreas _areas = new();

    private JsModule? _motion;

    /// <summary>The frame's own module (F110): the progress line, the compact header, the index, the reveals.</summary>
    private JsModule Motion => _motion ??= new JsModule(JS, FrameworkScripts.SiteShell);

    /// <summary>
    /// Attaches the motion after every render, not only the first: a page that just drew has reveals to observe, and
    /// the module hangs its listeners once. It is told what a place's address on this page starts with, so the
    /// address bar can follow the visitor, and the place the address names, so a page opened on one goes there.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        var here = Here;
        string? arrive = here.Section is null ? null : here.Turn is { } turn ? $"{here.Section}/{turn}" : here.Section;
        await Motion.InvokeVoidAsync("attach", Addresses?.Base(here.Page), arrive);
    }

    /// <summary>Takes the listeners down and lets the module go; a closed browser tab has no JS runtime to tell.</summary>
    public async ValueTask DisposeAsync()
    {
        _options.Changed -= Redraw;
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
