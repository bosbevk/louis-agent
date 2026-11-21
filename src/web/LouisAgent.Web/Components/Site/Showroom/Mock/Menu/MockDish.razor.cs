using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock.Menu;

/// <summary>
/// A dish on a mocked menu (F127.1): its name, a dotted leader to its price, and a few words under them.
/// </summary>
public partial class MockDish
{
    /// <summary>The dish's name: "Lamb shank".</summary>
    [Parameter, EditorRequired]
    public string Name { get; set; } = "";

    /// <summary>Its price: "R185".</summary>
    [Parameter, EditorRequired]
    public string Price { get; set; } = "";

    /// <summary>A few words about it; none for a dish its name says enough of.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
