using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace LouisAgent.Web.Docs.Contents;

/// <summary>
/// A docs page's foot (F164.1): the page before and the page after in reading order, so someone reading straight
/// through never needs the menu. Read from the site's <see cref="DocsContents"/>, the list the menu is drawn from.
/// </summary>
public partial class PageTurn : IDisposable
{
    /// <summary>The site's pages, in reading order.</summary>
    [Inject]
    private DocsContents Contents { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    /// <summary>The address being read.</summary>
    private string Here => new Uri(Nav.Uri).AbsolutePath;

    /// <summary>The page before, or none on the first.</summary>
    private DocsEntry? Before => Contents.FindBefore(Here);

    /// <summary>The page after, or none on the last.</summary>
    private DocsEntry? After => Contents.FindAfter(Here);

    /// <summary>Redraws when the visitor goes to another page.</summary>
    protected override void OnInitialized() => Nav.LocationChanged += Redraw;

    /// <summary>The page turns follow the visitor.</summary>
    private void Redraw(object? sender, LocationChangedEventArgs moved) => InvokeAsync(StateHasChanged);

    /// <summary>Stops listening when the foot leaves the page.</summary>
    public void Dispose() => Nav.LocationChanged -= Redraw;
}
