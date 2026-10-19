namespace LouisAgent.Packs.Shops;

/// <summary>A search the shop's catalogue couldn't answer, with a message the cook can act on.</summary>
public sealed class ShopCatalogueException(string message) : Exception(message);
