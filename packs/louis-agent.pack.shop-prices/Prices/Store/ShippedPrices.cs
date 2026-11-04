using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Prices.Store;

/// <summary>The tables the pack ships, in memory: the store when no MongoDB is set.</summary>
/// <remarks>A strategy of <see cref="IPriceStore"/> (Gang of Four): nothing to reach, so it can't fail.</remarks>
public sealed class ShippedPrices : IPriceStore
{
    /// <inheritdoc />
    public Task<IReadOnlyList<PriceFile>> LatestAsync(CancellationToken cancellationToken) =>
        Task.FromResult(PriceBook.Default.Files);

    /// <inheritdoc />
    public bool CanSave => false;

    /// <inheritdoc />
    public Task<IReadOnlyList<PriceFile>> MonthsAsync(string source, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PriceFile>>([.. PriceBook.Default.Files.Where(file => file.Source == source)]);

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">Always: the shipped tables are read-only; check <see cref="CanSave"/>.</exception>
    public Task SaveAsync(PriceFile file, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("The shipped price tables can't be changed; set SHOP_PRICES_MONGO_URL.");
}
