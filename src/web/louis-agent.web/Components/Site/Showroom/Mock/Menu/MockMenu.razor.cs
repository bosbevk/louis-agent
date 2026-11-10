using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock.Menu;

/// <summary>
/// A real menu on a mocked business's website (F127.1, Louis: "like having a real menu"): its courses in columns, as a
/// printed menu sets them.
/// </summary>
public partial class MockMenu
{
    /// <summary>Its courses.</summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = null!;
}
