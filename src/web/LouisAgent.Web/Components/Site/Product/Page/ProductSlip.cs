namespace LouisAgent.Web.Components.Site.Product.Page;

/// <summary>
/// The request the agent draws over the page as a card, such as a booking: what kind it is, its title, its lines,
/// and its status, which the scene changes once it is sent.
/// </summary>
public sealed record ProductSlip(string Topic, string Title, IReadOnlyList<SlipRow> Rows, string StatusLabel, string Status);
