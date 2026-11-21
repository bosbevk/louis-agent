using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock.Website;

/// <summary>
/// A page of a mock website (F127.2): its address on the business's site, its word in the menu and its sections. It
/// tells its mock it's there, and shows when the mock is on it.
/// </summary>
/// <remarks>
/// Composite, wired as Microsoft's TabSet example wires its tabs (Louis's pick of three): the mock cascades itself,
/// each page registers with it, and the mock draws its menu from the pages it holds. A site writes its pages as markup, in
/// the order its menu reads, and never says which is current; the mock hands that down as a cascading value.
/// </remarks>
/// <seealso href="https://learn.microsoft.com/aspnet/core/blazor/components/cascading-values-and-parameters#pass-data-across-a-component-hierarchy">Microsoft: pass data across a component hierarchy (TabSet)</seealso>
/// <seealso href="https://refactoring.guru/design-patterns/composite">Composite (Refactoring.Guru)</seealso>
public partial class MockPage : IDisposable
{
    /// <summary>The mock the page belongs to; none outside a mock, where the page simply shows.</summary>
    [CascadingParameter]
    private SiteMock? Mock { get; set; }

    /// <summary>The page the mock is on.</summary>
    [CascadingParameter(Name = SiteMock.CurrentPageName)]
    private string? Current { get; set; }

    /// <summary>The page's address on the business's site: <c>/</c>, <c>/rooms</c>, <c>/practice/wills</c>.</summary>
    [Parameter, EditorRequired]
    public string Path { get; set; } = "";

    /// <summary>The page's word in the menu, and its heading: "Rooms".</summary>
    [Parameter, EditorRequired]
    public string Label { get; set; } = "";

    /// <summary>Whether the menu lists it; a page opened from a card (Wills and estates) isn't.</summary>
    [Parameter]
    public bool InMenu { get; set; } = true;

    /// <summary>The page its way back leads to, for a page off the menu: <c>/practice</c>.</summary>
    [Parameter]
    public string? Parent { get; set; }

    /// <summary>The page's sections.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    /// <summary>Whether the mock is on this page; outside a mock, it is.</summary>
    private bool IsCurrent => Current is null || Current == Path;

    /// <summary>Whether this is the site's home, which the mock's hero opens instead of a heading.</summary>
    private bool IsHome => Path == MockPaths.Home;

    /// <summary>The parent's word in the menu, for the way back.</summary>
    private string ParentLabel => Mock?.LabelOf(Parent) ?? Parent ?? "";

    /// <summary>Tells the mock the page is there, in the order the site wrote it.</summary>
    protected override void OnInitialized() => Mock?.Register(this);

    /// <summary>Goes back to the parent page, as a visitor's click.</summary>
    private Task BackAsync() =>
        Parent is not null && Mock is not null ? Mock.GoAsync(Parent) : Task.CompletedTask;

    /// <summary>Takes the page off its mock's menu.</summary>
    public void Dispose()
    {
        Mock?.Unregister(this);
        GC.SuppressFinalize(this);
    }
}
