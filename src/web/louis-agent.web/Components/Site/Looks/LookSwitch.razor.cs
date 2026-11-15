using LouisAgent.Choices;
using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Scripts;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace LouisAgent.Web.Components.Site.Looks;

/// <summary>
/// The switch a visitor picks the look with (F151.3): the site's themes, by name, when it offers two or more, and the
/// two styles, Dark and Light, when it offers any. A site that offers none gets nothing, so its header is as it was.
/// The themes come from the site's areas, in the order the site named them, and the words from its
/// <see cref="ChatText"/>. A press writes the pick on the page and the browser keeps it, through the switch's module;
/// nothing is sent to the host. Drawn, it shows the look the page is already in, and it follows a pick made in
/// another tab.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0093-a-script-is-a-module-beside-its-component.md">ADR 0093: a script is a module beside its component</seealso>
public partial class LookSwitch
{
    /// <summary>The two styles, in the order the mockups show them.</summary>
    private static readonly SiteStyle[] Styles = [SiteStyle.Dark, SiteStyle.Light];

    [Inject] private IServiceProvider Services { get; set; } = null!;

    /// <summary>The site's words.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;

    [Inject] private IJSRuntime JS { get; set; } = null!;

    /// <summary>The theme that is on, by its id.</summary>
    private string? _theme;

    /// <summary>The style that is on.</summary>
    private SiteStyle _style = SiteLook.DefaultStyle;

    private JsModule? _module;

    /// <summary>What the module calls back on when another tab changes the pick.</summary>
    private DotNetObjectReference<LookSwitch>? _self;

    /// <summary>The switch's own module.</summary>
    private JsModule Module => _module ??= new JsModule(JS, FrameworkScripts.LookSwitch);

    /// <summary>What the site named, the themes among it; a frame drawn with no areas named has none.</summary>
    private SiteAreas? Areas => Services.GetService<SiteAreas>();

    /// <summary>The themes the site offers, in the order named.</summary>
    private IReadOnlyList<SiteTheme> Themes => Areas?.Themes ?? [];

    /// <summary>Starts on the defaults: the first theme named, and light.</summary>
    protected override void OnInitialized() => _theme = Areas?.DefaultTheme?.Id;

    /// <summary>
    /// Once drawn, learns the look the first paint already put the page in, and starts following other tabs. A site
    /// with no theme draws no switch and asks nothing.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || Themes.Count == 0) return;

        Take(await Module.InvokeAsync<PageLook?>("current"));
        _self = DotNetObjectReference.Create(this);
        await Module.InvokeVoidAsync("listen", _self);
        StateHasChanged();
    }

    /// <summary>A press on a theme: it is the one on, and the page and the browser are told.</summary>
    private async Task PickTheme(SiteTheme theme)
    {
        _theme = theme.Id;
        await Module.InvokeVoidAsync("pickTheme", theme.Id);
    }

    /// <summary>A press on a style: it is the one on, and the page and the browser are told.</summary>
    private async Task PickStyle(SiteStyle style)
    {
        _style = style;
        await Module.InvokeVoidAsync("pickStyle", style.Wire());
    }

    /// <summary>Another tab changed the pick, and the module already put it on this page: the switch follows.</summary>
    [JSInvokable]
    public void Followed(string? theme, string? style)
    {
        var look = new PageLook(theme, style);
        Take(look);
        StateHasChanged();
    }

    /// <summary>
    /// Takes what the page says is on: a theme the site offers, and a style that is one of the two. Anything else
    /// leaves what was on.
    /// </summary>
    /// <remarks>Guard clauses, one per part of the look.</remarks>
    private void Take(PageLook? look)
    {
        if (look is null) return;
        if (Themes.Any(theme => theme.Id == look.Theme)) _theme = look.Theme;
        if (SettingChoice.TryParse<SiteStyle>(look.Style, out var style) && style is { } known) _style = known;
    }

    /// <summary>Whether a theme's button is the one that is on, as aria-pressed says it.</summary>
    private string ThemePressed(SiteTheme theme) => Pressed(theme.Id == _theme);

    /// <summary>Whether a style's button is the one that is on, as aria-pressed says it.</summary>
    private string StylePressed(SiteStyle style) => Pressed(style == _style);

    /// <summary>A style's word on its button, in the site's language.</summary>
    private string StyleWord(SiteStyle style) => style == SiteStyle.Dark ? Text.StyleDark : Text.StyleLight;

    /// <summary>The value aria-pressed takes.</summary>
    private static string Pressed(bool on) => on ? "true" : "false";

    /// <summary>Stops following other tabs and lets the module go; a closed tab has no runtime to tell.</summary>
    public async ValueTask DisposeAsync()
    {
        _self?.Dispose();
        if (_module is null) return;
        try
        {
            await _module.InvokeVoidAsync("stop");
        }
        catch (JSDisconnectedException)
        {
        }

        await _module.DisposeAsync();
    }
}
