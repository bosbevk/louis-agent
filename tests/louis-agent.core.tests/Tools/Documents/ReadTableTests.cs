using LouisAgent.Core.Tools.Documents;

namespace LouisAgent.Core.Tests.Tools.Documents;

// F22 step 5: ReadTable gives the analyst a sheet's rows as a table, a page at a time, on the same library as ReadDocument.
public class ReadTableTests
{
    private string _root = null!;
    private DocumentTools _tools = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "read-table-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _tools = new DocumentTools(_root);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    [Test]
    public void ACsv_IsATable_WithItsSize()
    {
        File.WriteAllText(Path.Combine(_root, "verkope.csv"), "maand,bedrag\nSep,100\nOkt,250\n");

        string table = _tools.ReadTable("verkope.csv");

        Assert.That(table, Does.Contain("2 rows × 2 columns"));
        Assert.That(table, Does.Contain("| maand | bedrag |").And.Contain("| Okt | 250 |"));
    }

    [Test]
    public void ASheet_IsChosenByName_AndAnUnknownOneListsTheSheets()
    {
        DocumentFiles.Xlsx(Path.Combine(_root, "begroting.xlsx"), "Begroting", [["Item", "Bedrag"], ["Huur", "9500"]]);

        Assert.That(_tools.ReadTable("begroting.xlsx", sheet: "begroting"), Does.Contain("| Huur | 9500 |"), "names ignore case");
        Assert.That(_tools.ReadTable("begroting.xlsx", sheet: "Uitgawes"), Does.Contain("No sheet named 'Uitgawes'").And.Contain("Begroting"));
    }

    [Test]
    public void ManyRows_ComeInPages()
    {
        File.WriteAllText(Path.Combine(_root, "lank.csv"), "n\n" + string.Join("\n", Enumerable.Range(1, 120)));

        string first = _tools.ReadTable("lank.csv", maxRows: 50);
        Assert.That(first, Does.Contain("rows 1-50").And.Contain("startRow=50"));
        Assert.That(_tools.ReadTable("lank.csv", startRow: 100, maxRows: 50), Does.Contain("rows 101-120").And.Not.Contain("startRow="));
    }

    [Test]
    public void PipesInCells_DontBreakTheTable()
    {
        File.WriteAllText(Path.Combine(_root, "pyp.csv"), "a,b\nx|y,z\n");

        Assert.That(_tools.ReadTable("pyp.csv"), Does.Contain(@"| x\|y | z |"));
    }

    [Test]
    public void OtherFiles_AreRefused()
    {
        File.WriteAllText(Path.Combine(_root, "notas.txt"), "x");

        Assert.That(_tools.ReadTable("notas.txt"), Does.StartWith("ReadTable reads .xlsx, .xls, .csv"));
    }
}
