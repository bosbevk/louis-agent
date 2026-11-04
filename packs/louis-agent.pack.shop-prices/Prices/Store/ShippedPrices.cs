using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Prices.Store;

/// <summary>The tables the pack ships, in memory: the store when no MongoDB is set.</summary>
/// <remarks>A strategy of <see cref="IPriceStore"/> (Gang of Four): nothing to reach, so it can't fail.</remarks>
public sealed class ShippedPrices : IPriceStore
{
    /// <inheritdoc />
    public Task<IReadOnlyList<PriceFile>> LatestAsync(CancellationToken cancellationToken) =>
        Task.FromResult(PriceBook.Default.Files);
}
