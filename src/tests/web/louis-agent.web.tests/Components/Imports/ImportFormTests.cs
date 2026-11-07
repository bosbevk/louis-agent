using LouisAgent.Api.Contracts.Admin.Imports;
using LouisAgent.Web.Components.Imports;

namespace LouisAgent.Web.Tests.Components.Imports;

/// <summary>
/// The uploads page's form (F55): it holds the file chosen for each of an upload's slots and the last report. A
/// preview needs every slot filled, a save a clean preview that changes something, and choosing a file again forgets
/// the report, so a save never writes what the person hasn't seen.
/// </summary>
public class ImportFormTests
{
    /// <summary>The upload as the list names it: two files.</summary>
    private static readonly ImportRow Upload = new("prices", "Prices from a release.",
        [new ImportSlotRow("all-urban", "all urban areas"), new ImportSlotRow("provinces", "provinces")]);

    /// <summary>A clean preview that would store two months.</summary>
    private static readonly ImportReportReply Clean = new("June 2026 to July 2026", 2, false, [], []);

    /// <summary>A form with both files chosen.</summary>
    private static ImportForm Filled()
    {
        var form = new ImportForm(Upload);
        form.Choose("all-urban", "urban.xlsx", [1]);
        form.Choose("provinces", "provinces.xlsx", [2]);
        return form;
    }

    /// <summary>A preview waits for every slot's file.</summary>
    [Test]
    public void APreview_NeedsEveryFile()
    {
        var form = new ImportForm(Upload);
        form.Choose("all-urban", "urban.xlsx", [1]);

        Assert.That(form.CanPreview, Is.False);
        Assert.That(Filled().CanPreview, Is.True);
    }

    /// <summary>The request carries the files in the slots' order, by slot name.</summary>
    [Test]
    public void TheRequest_CarriesTheFilesInSlotOrder()
    {
        var form = new ImportForm(Upload);
        form.Choose("provinces", "provinces.xlsx", [2]);
        form.Choose("all-urban", "urban.xlsx", [1]);

        var files = form.Request().Files!;

        Assert.That(files.Select(f => (f.Slot, f.FileName)),
            Is.EqualTo(new[] { ("all-urban", "urban.xlsx"), ("provinces", "provinces.xlsx") }));
        Assert.That(form.FileName("provinces"), Is.EqualTo("provinces.xlsx"));
    }

    /// <summary>A save needs a clean preview that changes something, and isn't offered twice.</summary>
    [Test]
    public void ASave_NeedsACleanPreviewThatChangesSomething()
    {
        var form = Filled();
        Assert.That(form.CanSave, Is.False);

        form.Report = Clean;
        Assert.That(form.CanSave, Is.True);

        form.Report = Clean with { Problems = ["A month is missing."] };
        Assert.That(form.CanSave, Is.False);

        form.Report = Clean with { Changes = 0 };
        Assert.That(form.CanSave, Is.False);

        form.Report = Clean with { Saved = true };
        Assert.That(form.CanSave, Is.False);
    }

    /// <summary>Choosing a file again forgets the report: the next save must follow a new preview.</summary>
    [Test]
    public void ChoosingAFileAgain_ForgetsTheReport()
    {
        var form = Filled();
        form.Report = Clean;

        form.Choose("provinces", "other.xlsx", [3]);

        Assert.That((form.Report, form.CanSave), Is.EqualTo(((ImportReportReply?)null, false)));
    }
}
