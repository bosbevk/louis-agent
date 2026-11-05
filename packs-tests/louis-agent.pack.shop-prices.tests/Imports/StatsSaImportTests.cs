using LouisAgent.Composition.Packs.Imports;
using LouisAgent.Choices;
using LouisAgent.Packs.ShopPrices.Imports;
using LouisAgent.Packs.ShopPrices.Prices.Files;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa;
using LouisAgent.Packs.ShopPrices.Prices.Store;
using LouisAgent.Packs.ShopPrices.Tests.Prices.Store;

namespace LouisAgent.Packs.ShopPrices.Tests.Imports;

/// <summary>
/// The Stats SA upload, on the trimmed release (June to August 2026) and a store in memory: the preview sets each month
/// beside the store, Save writes the new and the corrected ones only, and the next estimate prices from them.
/// </summary>
public class StatsSaImportTests
{
    private static ImportFile File(string slot, string fixture) => new(slot, fixture,
        System.IO.File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", fixture)));

    private static readonly IReadOnlyList<ImportFile> Release =
        [File("all-urban", "statssa-all-urban.xlsx"), File("provinces", "statssa-provinces.xlsx")];

    private static (StatsSaImport Import, PriceBookCache Books) Import(MemoryPriceStore store)
    {
        var books = new PriceBookCache(store, TimeProvider.System);
        return (new StatsSaImport(store, books, StatsSaNames.Default), books);
    }

    /// <summary>The release's months as the converter makes them, to put in a store as if saved before.</summary>
    private static async Task<IReadOnlyList<PriceFile>> MonthsOfTheRelease()
    {
        var store = new MemoryPriceStore();
        await Import(store).Import.SaveAsync(Release, CancellationToken.None);
        return store.Saved;
    }

    /// <summary>It is named for the pack and takes the two files by their slots.</summary>
    [Test]
    public void ItTakesTheTwoFiles()
    {
        var (import, _) = Import(new MemoryPriceStore());

        Assert.That(import.Name, Is.EqualTo(ShopPricesToolset.ShopPrices.Wire()));
        Assert.That(import.Files.Select(f => f.Name), Is.EqualTo(new[] { "all-urban", "provinces" }));
    }

    /// <summary>Against an empty store every month is new; the preview saves nothing.</summary>
    [Test]
    public async Task ThePreview_ListsEveryMonthAsNew_AndSavesNothing()
    {
        var store = new MemoryPriceStore();

        var report = await Import(store).Import.PreviewAsync(Release, CancellationToken.None);

        Assert.That(report.Problems, Is.Empty);
        Assert.That(report.Period, Is.EqualTo("June 2026 to August 2026"));
        Assert.That(report.Changes, Is.EqualTo(3));
        Assert.That(report.Saved, Is.False);
        Assert.That(report.Facts, Does.Contain(new ImportFact("New months", "3 (June 2026, July 2026, August 2026)")));
        Assert.That(report.Facts, Does.Contain(new ImportFact("Products in August 2026", "2")));
        Assert.That(store.Saved, Is.Empty);
    }

    /// <summary>Save writes the months, the next estimate reads them at once, and the same files again change nothing.</summary>
    [Test]
    public async Task Save_WritesTheMonths_AndTheSameFilesAgainChangeNothing()
    {
        var store = new MemoryPriceStore();
        var (import, books) = Import(store);
        await books.GetAsync(CancellationToken.None);

        var saved = await import.SaveAsync(Release, CancellationToken.None);
        var again = await import.SaveAsync(Release, CancellationToken.None);

        Assert.That(saved.Saved, Is.True);
        Assert.That(store.Saved.Select(m => m.AsOf.Month), Is.EqualTo(new[] { 6, 7, 8 }));
        Assert.That((await books.GetAsync(CancellationToken.None)).Files[0].AsOf, Is.EqualTo(new DateOnly(2026, 8, 1)),
            "the cache forgot the empty book it held");
        Assert.That((again.Changes, again.Saved, store.Saved.Count), Is.EqualTo((0, false, 3)));
        Assert.That(again.Facts, Does.Contain(new ImportFact("Months stored already, unchanged", "3")));
    }

    /// <summary>A stored month Stats SA has since corrected is listed with its change and rewritten; the rest stay.</summary>
    [Test]
    public async Task ACorrectedMonth_IsListed_AndOnlyItIsWritten()
    {
        var months = await MonthsOfTheRelease();
        var july = months[1];
        var rice = july.Products[0];
        var oldPack = rice.Packs[0] with
        {
            Prices = new Dictionary<string, decimal>(rice.Packs[0].Prices) { ["national"] = 26.99m },
        };
        var storedJuly = july with { Products = [rice with { Packs = [oldPack, .. rice.Packs.Skip(1)] }, .. july.Products.Skip(1)] };
        var store = new MemoryPriceStore(true, months[0], storedJuly, months[2]);

        var report = await Import(store).Import.SaveAsync(Release, CancellationToken.None);

        Assert.That(report.Changes, Is.EqualTo(1));
        Assert.That(report.Facts, Does.Contain(new ImportFact("Corrected: July 2026",
            "1 changed: White Rice 1 Kilogram in South Africa: R26.99 to R27.50")));
        Assert.That(store.Saved.Select(m => m.AsOf), Is.EqualTo(new[] { new DateOnly(2026, 7, 1) }));
    }

    /// <summary>A missing file, or a store that can't keep anything, is a problem, and nothing is saved.</summary>
    [Test]
    public async Task AMissingFileOrAStoreThatCantSave_IsAProblem()
    {
        var missing = await Import(new MemoryPriceStore()).Import.SaveAsync([Release[0]], CancellationToken.None);
        var readOnly = new MemoryPriceStore(canSave: false);
        var cantSave = await Import(readOnly).Import.SaveAsync(Release, CancellationToken.None);

        Assert.That(missing.Problems, Is.EqualTo(new[] { "No provinces file was given." }));
        Assert.That(missing.Saved, Is.False);
        Assert.That(cantSave.Problems.Single(), Does.Contain("SHOP_PRICES_MONGO_URL"));
        Assert.That((cantSave.Saved, readOnly.Saved.Count), Is.EqualTo((false, 0)));
        Assert.That(cantSave.Period, Is.EqualTo("June 2026 to August 2026"), "the preview still shows the files");
    }
}
