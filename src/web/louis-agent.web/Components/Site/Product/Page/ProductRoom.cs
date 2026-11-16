namespace LouisAgent.Web.Components.Site.Product.Page;

/// <summary>
/// One card on a business's page in the product window: its photo, its name and a line under it. The one the agent
/// opens for the visitor is picked, and stands forward when it does.
/// </summary>
public sealed record ProductRoom(string Image, string Title, string Line, bool Picked = false);
