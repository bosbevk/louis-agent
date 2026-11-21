using LouisAgent.Web.Components.Site.Showroom.Mock.Website;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock.Foot;

/// <summary>
/// The end of a mocked business's website: its name again, its menu, and the facts from its hero. A real website
/// ends with a foot, and without one the mock's last page just stopped.
/// </summary>
/// <remarks>
/// It mirrors the header: the same brand and the same menu, handed down by the mock, and a click is told to the mock
/// as the header's is. It writes no word of its own, so everything on it is something the business's agent knows.
/// </remarks>
public partial class MockFoot
{
    /// <summary>The business's mark; none for a business with no mark.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }

    /// <summary>The business's name.</summary>
    [Parameter, EditorRequired]
    public string Business { get; set; } = "";

    /// <summary>The few words under its name; none for a business with no tagline.</summary>
    [Parameter]
    public string? Tagline { get; set; }

    /// <summary>The pages its menu lists.</summary>
    [Parameter]
    public IEnumerable<MockPage> Menu { get; set; } = [];

    /// <summary>The facts its hero gave, listed again.</summary>
    [Parameter]
    public IReadOnlyList<string> Facts { get; set; } = [];

    /// <summary>Told which page a click chose, by its address.</summary>
    [Parameter]
    public EventCallback<string> Went { get; set; }
}
