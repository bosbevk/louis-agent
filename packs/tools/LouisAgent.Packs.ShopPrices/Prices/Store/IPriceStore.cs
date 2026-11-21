using LouisAgent.Packs.ShopPrices.Prices.Files;

namespace LouisAgent.Packs.ShopPrices.Prices.Store;

/// <summary>
/// Where the price tables are kept: it hands over each source's newest month, in the order a tie is settled (Stats
/// SA's first), and, for an upload, every month of a source and a place to save one.
/// </summary>
/// <remarks>
/// Repository (Fowler, PoEAA) with two strategies (Gang of Four): <see cref="ShippedPrices"/> in memory and
/// <see cref="MongoPriceStore"/> in MongoDB, picked once from the settings, so the tool never knows which it reads.
/// </remarks>
public interface IPriceStore
{
    /// <summary>Each source's newest table, Stats SA's first.</summary>
    Task<IReadOnlyList<PriceFile>> LatestAsync(CancellationToken cancellationToken);

    /// <summary>Whether it can keep an uploaded table: MongoDB can, the shipped tables in memory can't (F55).</summary>
    bool CanSave { get; }

    /// <summary>Every month it holds of one source, oldest first, so an upload can tell new months from corrected ones.</summary>
    Task<IReadOnlyList<PriceFile>> MonthsAsync(string source, CancellationToken cancellationToken);

    /// <summary>Keeps a table as its source's month: added when the month is new, replacing it when it is stored.</summary>
    Task SaveAsync(PriceFile file, CancellationToken cancellationToken);
}
