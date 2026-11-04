using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Prices.Store;

/// <summary>
/// Where the price tables are kept: it hands over each source's newest month, in the order a tie is settled (Stats
/// SA's first).
/// </summary>
/// <remarks>
/// Repository (Fowler, PoEAA) with two strategies (Gang of Four): <see cref="ShippedPrices"/> in memory and
/// <see cref="MongoPriceStore"/> in MongoDB, picked once from the settings, so the tool never knows which it reads.
/// </remarks>
public interface IPriceStore
{
    /// <summary>Each source's newest table, Stats SA's first.</summary>
    Task<IReadOnlyList<PriceFile>> LatestAsync(CancellationToken cancellationToken);
}
