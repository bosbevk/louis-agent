using LouisAgent.Packs.ShopPrices.Prices.Regions;
using LouisAgent.Packs.ShopPrices.Prices.StatsSa.Sheets;

namespace LouisAgent.Packs.ShopPrices.Tests.Prices.StatsSa.Sheets;

/// <summary>
/// Stats SA's two spreadsheets read into rows: one per product, pack, region and month with a price, from a trimmed
/// copy of the August 2026 release (three months, a few products, prices as text in one sheet and as numbers in the
/// other, as Stats SA writes them).
/// </summary>
public class StatsSaSheetsTests
{
    private static string Fixture(string name) => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name);

    private static StatsSaRows ReadFixtures()
    {
        using var allUrban = File.OpenRead(Fixture("statssa-all-urban.xlsx"));
        using var provinces = File.OpenRead(Fixture("statssa-provinces.xlsx"));
        return StatsSaSheets.Read(allUrban, provinces);
    }

    /// <summary>Every priced cell is a row; a blank cell is none, so Basmati, never priced here, has no row.</summary>
    [Test]
    public void EveryPricedCell_IsARow_AndABlankOneIsNone()
    {
        var read = ReadFixtures();

        Assert.That(read.Problems, Is.Empty);
        Assert.That(read.Rows, Has.Count.EqualTo(13));
        Assert.That(read.Rows.Select(r => r.Code), Has.None.EqualTo("01111202"));
        Assert.That(read.Rows.Where(r => r.Code == "01171101" && r.Region == Region.National).Select(r => r.Month),
            Is.EqualTo(new[] { new DateOnly(2026, 7, 1) }));
    }

    /// <summary>
    /// A row keeps the code, the name and the size as Stats SA wrote them; the month comes from its column, the region
    /// from the sheet (the country) or the province column.
    /// </summary>
    [Test]
    public void ARow_KeepsWhatStatsSaWrote_WithItsMonthAndRegion()
    {
        var read = ReadFixtures();

        Assert.That(read.Rows, Does.Contain(new StatsSaRow("01111201", "White Rice", "1 Kilogram", Region.National,
            new DateOnly(2026, 8, 1), 27.86m)));
        Assert.That(read.Rows, Does.Contain(new StatsSaRow("01111201", "White Rice", "1 Kilogram",
            Region.KwaZuluNatal, new DateOnly(2026, 7, 1), 28.10m)));
        Assert.That(read.Rows, Does.Contain(new StatsSaRow("01171101", "Onions  - fresh", "1,5 Kilogram",
            Region.Gauteng, new DateOnly(2026, 8, 1), 25.49m)));
    }

    /// <summary>A file that isn't a spreadsheet is a problem that names the sheet, never a thrown exception.</summary>
    [Test]
    public void AFileThatIsNoSpreadsheet_IsAProblem()
    {
        using var allUrban = new MemoryStream("not a spreadsheet"u8.ToArray());
        using var provinces = File.OpenRead(Fixture("statssa-provinces.xlsx"));

        var read = StatsSaSheets.Read(allUrban, provinces);

        Assert.That(read.Problems, Has.Count.EqualTo(1));
        Assert.That(read.Problems[0], Does.Contain("all urban areas"));
        Assert.That(read.Rows.Select(r => r.Region), Has.None.EqualTo(Region.National));
    }

    /// <summary>
    /// The provinces' file given where the all-urban one belongs still reads (its columns are all there), but the
    /// all-urban file in the provinces' slot lacks the province column, which is named.
    /// </summary>
    [Test]
    public void ASheetWithoutAColumnItNeeds_NamesTheColumn()
    {
        using var allUrban = File.OpenRead(Fixture("statssa-all-urban.xlsx"));
        using var provinces = File.OpenRead(Fixture("statssa-all-urban.xlsx"));

        var read = StatsSaSheets.Read(allUrban, provinces);

        Assert.That(read.Problems, Has.Count.EqualTo(1));
        Assert.That(read.Problems[0], Does.Contain("H09").And.Contain("provinces"));
    }
}
