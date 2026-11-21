using LouisAgent.Web.Components.Site.Showroom.Mock.Website;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock.Head;

/// <summary>
/// A mocked business's header (F127.1): its mark, its name and tagline, and its menu of pages. It draws what its mock
/// tells it and tells the mock where a click went; the brand it wears comes from its mock's classes (F127.2).
/// </summary>
/// <remarks>
/// Apart from <see cref="SiteMock"/> since F127.2, when each brand got a header of its own: the header's styles
/// (a band, a rule or a double line, and each brand's wordmark) sit beside it, and the mock keeps its flow.
/// </remarks>
public partial class MockHead
{
    /// <summary>The business's mark, drawn for it; none for a business without one.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }

    /// <summary>The business's name, its wordmark.</summary>
    [Parameter, EditorRequired]
    public string Business { get; set; } = "";

    /// <summary>The few words under its name; none for a business without a tagline.</summary>
    [Parameter]
    public string? Tagline { get; set; }

    /// <summary>The pages its menu lists, in order.</summary>
    [Parameter]
    public IEnumerable<MockPage> Menu { get; set; } = [];

    /// <summary>The page the website is on.</summary>
    [Parameter]
    public string Current { get; set; } = MockPaths.Home;

    /// <summary>Told where a visitor's click went, by the page's address.</summary>
    [Parameter]
    public EventCallback<string> Went { get; set; }

    /// <summary>A menu word's classes: the page the website is on is marked.</summary>
    private string NavClass(MockPage page) => page.Path == Current ? "mock-nav-item is-current" : "mock-nav-item";

    /// <summary>The current page's menu word says so, for a screen reader.</summary>
    private string? CurrentMark(MockPage page) => page.Path == Current ? "page" : null;
}
