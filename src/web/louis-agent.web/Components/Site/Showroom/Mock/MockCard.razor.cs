using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Site.Showroom.Mock;

/// <summary>
/// A card on a mocked business's website (F127.1): a room, a dish, an area of practice, with its title, a note such as
/// its price, and a few words, at its own address on the business's site (<c>/rooms/garden</c>), so the business's
/// agent can show it.
/// </summary>
public partial class MockCard
{
    /// <summary>Its address on the business's site: <c>/rooms/garden</c>.</summary>
    [Parameter, EditorRequired]
    public string Path { get; set; } = "";

    /// <summary>Its title: "The garden room".</summary>
    [Parameter, EditorRequired]
    public string Title { get; set; } = "";

    /// <summary>A note beside the title, such as its price; none for a card without one.</summary>
    [Parameter]
    public string? Note { get; set; }

    /// <summary>Its few words; none for a card of a title and a note alone.</summary>
    [Parameter]
    public RenderFragment? ChildContent { get; set; }
}
