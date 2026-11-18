using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace LouisAgent.Web.Docs.Contents;

/// <summary>
/// The docs layout's left column (F162.1): the site's pages under their groups, in reading order, with the page being
/// read marked, also when the address is one of its sections'.
/// </summary>
/// <remarks>
/// Drawn from the site's <see cref="DocsContents"/>, the list its page turns read too. It marks the page by asking the
/// contents which page the address is on, so a section's own address (ADR 0107) still marks its page.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0107-every-section-has-an-address.md">ADR 0107: every section has an address</seealso>
public partial class DocsMenu : IDisposable
{
    /// <summary>The site's pages, in reading order.</summary>
    [Inject]
    private DocsContents Contents { get; set; } = default!;

    [Inject]
    private NavigationManager Nav { get; set; } = default!;

    /// <summary>The page being read, asked of the contents from the address.</summary>
    private DocsEntry? Current => Contents.FindEntry(new Uri(Nav.Uri).AbsolutePath);

    /// <summary>"page" on the page being read, which marks it for the eye and a screen reader; nothing elsewhere.</summary>
    private string? CurrentOf(DocsEntry page) => page == Current ? "page" : null;

    /// <summary>Redraws when the visitor goes to another page, so the mark follows.</summary>
    protected override void OnInitialized() => Nav.LocationChanged += Redraw;

    /// <summary>The mark follows the visitor.</summary>
    private void Redraw(object? sender, LocationChangedEventArgs moved) => InvokeAsync(StateHasChanged);

    /// <summary>Stops listening when the menu leaves the page.</summary>
    public void Dispose() => Nav.LocationChanged -= Redraw;
}
