using LouisAgent.Web.Components.Site.Product.Page;
using LouisAgent.Web.Components.Site.Product.Scene;

namespace LouisAgent.Web.Components.Site.Product;

/// <summary>
/// One business in the product window: its page (address, name, menu and cards), the steps its agent plays in the
/// chat beside it, the line under the chat and its sign, and, when it takes a request, the card it draws for it.
/// </summary>
public sealed record ProductScene(
    string Address,
    string Name,
    IReadOnlyList<string> Menu,
    IReadOnlyList<ProductRoom> Rooms,
    IReadOnlyList<SceneStep> Steps,
    string Foot,
    string Sign)
{
    /// <summary>The request the agent draws over the page; none for a scene that takes no request.</summary>
    public ProductSlip? Slip { get; init; }
}
