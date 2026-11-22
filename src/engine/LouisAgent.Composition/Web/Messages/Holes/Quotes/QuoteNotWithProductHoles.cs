namespace LouisAgent.Composition.Web.Messages.Holes.Quotes;

/// <summary>The page, the product the quote must follow, and how close it must stand.</summary>
public sealed record QuoteNotWithProductHoles(string Url, string Product, int Window);
