using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.Store;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices.Store;

/// <summary>
/// A price store in memory for the upload's and the cache's tests: the months a test gives it, by source and month,
/// what was saved, and whether it may save at all.
/// </summary>
public sealed class MemoryPriceStore(bool canSave = true, params PriceFile[] months) : IPriceStore
{
    private readonly List<PriceFile> _months = [.. months];

    /// <summary>Every month saved, in order.</summary>
    public List<PriceFile> Saved { get; } = [];

    /// <inheritdoc />
    public bool CanSave => canSave;

    /// <inheritdoc />
    public Task<IReadOnlyList<PriceFile>> LatestAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PriceFile>>(
            [.. _months.GroupBy(m => m.Source).Select(source => source.MaxBy(m => m.AsOf)!)]);

    /// <inheritdoc />
    public Task<IReadOnlyList<PriceFile>> MonthsAsync(string source, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<PriceFile>>([.. _months.Where(m => m.Source == source).OrderBy(m => m.AsOf)]);

    /// <inheritdoc />
    public Task SaveAsync(PriceFile file, CancellationToken cancellationToken)
    {
        _months.RemoveAll(m => m.Source == file.Source && m.AsOf == file.AsOf);
        _months.Add(file);
        Saved.Add(file);
        return Task.CompletedTask;
    }
}
