using LouisAgent.Web.Components.Areas;
using LouisAgent.Web.Components.Areas.Themes;
using LouisAgent.Web.Text;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Looks;

/// <summary>
/// The switch a visitor picks the look with (F151.3): the site's themes, by name, when it offers two or more, and the
/// two styles, Dark and Light, when it offers any. A site that offers none gets nothing, so its header is as it was.
/// The themes come from the site's areas, in the order the site named them, and the words from its
/// <see cref="ChatText"/>. Until the browser says which look is on, the pressed pair is the defaults: the first theme
/// named, and light.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0124-a-sites-look-is-a-theme-and-a-style.md">ADR 0124: a site's look is a theme and a style</seealso>
public partial class LookSwitch
{
    /// <summary>The two styles, in the order the mockups show them.</summary>
    private static readonly SiteStyle[] Styles = [SiteStyle.Dark, SiteStyle.Light];

    /// <summary>What the site named, the themes among it.</summary>
    [Inject]
    private SiteAreas Areas { get; set; } = null!;

    /// <summary>The site's words.</summary>
    [Inject]
    private ChatText Text { get; set; } = null!;

    /// <summary>The theme that is on, by its id.</summary>
    private string? _theme;

    /// <summary>The style that is on.</summary>
    private SiteStyle _style = SiteLook.DefaultStyle;

    /// <summary>The themes the site offers, in the order named.</summary>
    private IReadOnlyList<SiteTheme> Themes => Areas.Themes;

    /// <summary>Starts on the defaults: the first theme named, and light.</summary>
    protected override void OnInitialized() => _theme = Areas.DefaultTheme?.Id;

    /// <summary>Whether a theme's button is the one that is on, as aria-pressed says it.</summary>
    private string ThemePressed(SiteTheme theme) => Pressed(theme.Id == _theme);

    /// <summary>Whether a style's button is the one that is on, as aria-pressed says it.</summary>
    private string StylePressed(SiteStyle style) => Pressed(style == _style);

    /// <summary>A style's word on its button, in the site's language.</summary>
    private string StyleWord(SiteStyle style) => style == SiteStyle.Dark ? Text.StyleDark : Text.StyleLight;

    /// <summary>The value aria-pressed takes.</summary>
    private static string Pressed(bool on) => on ? "true" : "false";
}
