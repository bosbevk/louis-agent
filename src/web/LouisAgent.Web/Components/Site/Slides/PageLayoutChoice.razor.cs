using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Slides;

/// <summary>
/// A page that wants a layout other than the site's says so with this (F118, Louis's option 2: the site's default,
/// and a page may override it): a long list reads better as a flowing page on a site of slides. It tells the frame as
/// it draws and takes its choice back when the page goes, so the next page gets the site's default again.
/// </summary>
public partial class PageLayoutChoice : IDisposable
{
    /// <summary>What the page asks of the frame, from the frame's cascade.</summary>
    [CascadingParameter] public PageOptions? Options { get; set; }

    /// <summary>The layout this page wants.</summary>
    [Parameter, EditorRequired] public PageLayout Layout { get; set; }

    /// <summary>Tells the frame.</summary>
    protected override void OnParametersSet() => Options?.Choose(Layout);

    /// <summary>Gives the site's default back when the page goes.</summary>
    public void Dispose() => Options?.Choose(null);
}
