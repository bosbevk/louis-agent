using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// The agent's widget in the corner of a mocked business's website (F127.1): where its customers would meet it, under
/// a head in the business's own brand.
/// </summary>
public partial class MockWidget
{
    /// <summary>The business's mark, before the label; none for a business without one.</summary>
    [Parameter]
    public RenderFragment? Mark { get; set; }

    /// <summary>The widget's title: "The Bakery · ask us".</summary>
    [Parameter]
    public string Label { get; set; } = "";

    /// <summary>The agent: its recorded chat, or a live one.</summary>
    [Parameter, EditorRequired]
    public RenderFragment ChildContent { get; set; } = null!;
}
