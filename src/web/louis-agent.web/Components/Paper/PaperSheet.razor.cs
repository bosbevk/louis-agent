using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Paper;

/// <summary>
/// A page that prints as itself: on paper it is an A4 sheet in black on white, with the site's frame, its menu and
/// its corner left off, no button, since paper can't press one, and the page's number at each sheet's foot. On a
/// screen it changes nothing.
/// </summary>
/// <remarks>
/// The sheet's rules are the framework's paper stylesheet (<c>web/paper/sheet</c>), which a site lists among its
/// styles: the colours are set on the page's root and the frame is the shell's, and a component's own styles reach
/// neither. Each control's own layout on paper sits beside it, as <see cref="PaperLink"/>'s does.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0125-a-site-is-configured-framework-components.md">ADR 0125: a site is configured framework components</seealso>
public partial class PaperSheet
{
    /// <summary>The page that prints.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
