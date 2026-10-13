using LouisAgent.Core.Tools.Workspace;

namespace LouisAgent.Core.Tests.Tools.Documents;

// F22 step 3: ReadDocument gives the reader PDFs, Word documents and spreadsheets as text, with the file tools' guards.
public class ReadDocumentTests
{
    private string _root = null!;
    private WorkspaceTools _tools = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "read-document-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
        _tools = new WorkspaceTools(_root);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_root, recursive: true);

    private string At(string name) => Path.Combine(_root, name);

    [Test]
    public void APdf_IsReadPageByPage()
    {
        File.WriteAllBytes(At("verslag.pdf"), DocumentFiles.Pdf(["Inkomste vir Oktober"], ["Uitgawes vir Oktober"]));

        string text = _tools.ReadDocument("verslag.pdf");

        Assert.That(text, Does.Contain("--- Page 1 ---").And.Contain("Inkomste vir Oktober"));
        Assert.That(text, Does.Contain("--- Page 2 ---").And.Contain("Uitgawes vir Oktober"));
    }

    [Test]
    public void APdfWithNoText_SaysSo()
    {
        File.WriteAllBytes(At("skandeer.pdf"), DocumentFiles.Pdf([]));

        Assert.That(_tools.ReadDocument("skandeer.pdf"), Does.Contain("no text").And.Contain("no OCR"));
    }

    [Test]
    public void AWordDocument_HasItsParagraphsAndTables()
    {
        DocumentFiles.Docx(At("brief.docx"), ["Beste Louis,", "Hier is die syfers."], [["Maand", "Bedrag"], ["Oktober", "R1 200"]]);

        string text = _tools.ReadDocument("brief.docx");

        Assert.That(text, Does.Contain("Beste Louis,").And.Contain("Hier is die syfers."));
        Assert.That(text, Does.Contain("Oktober\tR1 200"), "table cells, tab-separated");
    }

    [Test]
    public void ASpreadsheet_HasItsSheetsAndRows()
    {
        DocumentFiles.Xlsx(At("begroting.xlsx"), "Begroting", [["Item", "Bedrag"], ["Huur", "9500"]]);

        string text = _tools.ReadDocument("begroting.xlsx");

        Assert.That(text, Does.Contain("--- Sheet Begroting ---").And.Contain("Item\tBedrag").And.Contain("Huur\t9500"));
    }

    [Test]
    public void ACsv_HasItsRows()
    {
        File.WriteAllText(At("data.csv"), "naam,stad\nAnna,Pretoria\n");

        Assert.That(_tools.ReadDocument("data.csv"), Does.Contain("Anna\tPretoria"));
    }

    [Test]
    public void ALongDocument_ComesInParts()
    {
        File.WriteAllText(At("lank.csv"), string.Join("\n", Enumerable.Range(1, 400).Select(i => $"ry {i},waarde {i}")));

        string first = _tools.ReadDocument("lank.csv", maxChars: 500);
        Assert.That(first, Does.Contain("Call ReadDocument with startIndex=500"));
        Assert.That(_tools.ReadDocument("lank.csv", maxChars: 500, startIndex: 500), Does.Not.StartWith("ry 1\t"));
    }

    [Test]
    public void OtherFiles_AreSentToReadWorkspaceFile()
    {
        File.WriteAllText(At("notas.txt"), "x");

        Assert.That(_tools.ReadDocument("notas.txt"), Does.Contain("Use ReadWorkspaceFile"));
    }

    [Test]
    public void ADamagedFile_SaysWhy()
    {
        File.WriteAllText(At("stukkend.pdf"), "not a pdf");

        Assert.That(_tools.ReadDocument("stukkend.pdf"), Does.StartWith("Couldn't read stukkend.pdf as a document"));
    }

    [Test]
    public void SecretFiles_StayBlocked()
    {
        File.WriteAllText(At(".env.csv"), "KEY,secret");

        Assert.That(_tools.ReadDocument(".env.csv"), Does.Not.Contain("secret"));
    }
}
