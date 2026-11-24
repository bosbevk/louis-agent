using LouisAgent.Packs.Docs.Tools;

namespace LouisAgent.Packs.Docs.Tests.Tools;

/// <summary>
/// The three tools over a small docs folder: the top is listed folders first, a doc is read whole, a search names the
/// doc and the line, and nothing outside the root, nothing that isn't a doc, and nothing too big is read.
/// </summary>
public class DocsToolsTests
{
    private string _root = null!;
    private DocsTools _tools = null!;

    /// <summary>A docs folder with an index, an ADR folder, a picture and a file outside it.</summary>
    [SetUp]
    public void MakeTheDocs()
    {
        _root = Path.Combine(Path.GetTempPath(), $"docs-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_root, "adr"));
        File.WriteAllText(Path.Combine(_root, "README.md"), "# The docs\n\nStart at the [ADRs](adr/README.md).\n");
        File.WriteAllText(Path.Combine(_root, "adr", "0001-first.md"),
            "# 0001. The first decision\n\nPacks stay convertible.\n");
        File.WriteAllBytes(Path.Combine(_root, "film.webp"), [1, 2, 3]);
        File.WriteAllText(Path.Combine(Path.GetTempPath(), $"outside-{Path.GetFileName(_root)}.md"), "secret\n");
        _tools = new DocsTools(_root);
    }

    [TearDown]
    public void RemoveTheDocs()
    {
        Directory.Delete(_root, recursive: true);
        File.Delete(Path.Combine(Path.GetTempPath(), $"outside-{Path.GetFileName(_root)}.md"));
    }

    /// <summary>The top lists its folders first, then its docs, and never the picture.</summary>
    [Test]
    public void ListingTheTop_ShowsFoldersThenDocs_NeverAPicture()
    {
        Assert.That(_tools.ListDocs(), Is.EqualTo("adr/\nREADME.md"));
        Assert.That(_tools.ListDocs("adr"), Is.EqualTo("adr/0001-first.md"));
    }

    /// <summary>A folder that isn't there says so, as the model should hear it.</summary>
    [Test]
    public void ListingAMissingFolder_SaysSo() =>
        Assert.That(_tools.ListDocs("nowhere"), Does.Contain("nowhere").And.Contain("ListDocs"));

    /// <summary>A doc is read whole, by the path the listing showed.</summary>
    [Test]
    public void ADoc_IsReadWhole() =>
        Assert.That(_tools.ReadDoc("adr/0001-first.md"), Does.StartWith("# 0001.").And.Contain("convertible"));

    /// <summary>
    /// A doc reads the same on every machine: a Windows checkout's CRLF endings reach the model as LF, as a Linux one's
    /// do, so a recorded request still answers.
    /// </summary>
    [Test]
    public void ADocWithWindowsLineEndings_IsReadWithLf()
    {
        File.WriteAllText(Path.Combine(_root, "windows.md"), "# Windows\r\n\r\nOne line.\r\n");

        Assert.That(_tools.ReadDoc("windows.md"), Is.EqualTo("# Windows\n\nOne line.\n"));
    }

    /// <summary>A path outside the root, a picture, a missing doc and a huge doc are each refused, with why.</summary>
    [Test]
    public void WhatIsNotADocToRead_IsRefusedWithItsReason()
    {
        File.WriteAllText(Path.Combine(_root, "huge.md"), new string('x', (int)DocsTools.MostBytes + 1));

        Assert.Multiple(() =>
        {
            Assert.That(_tools.ReadDoc($"../outside-{Path.GetFileName(_root)}.md"), Does.Contain("outside the docs"));
            Assert.That(_tools.ReadDoc("film.webp"), Does.Contain("not a doc"));
            Assert.That(_tools.ReadDoc("adr/0002-nothing.md"), Does.Contain("no doc at"));
            Assert.That(_tools.ReadDoc("huge.md"), Does.Contain("over 1 MB"));
        });
    }

    /// <summary>A search names the doc and the line, case ignored; nothing found says so.</summary>
    [Test]
    public void ASearch_NamesTheDocAndTheLine()
    {
        Assert.That(_tools.SearchDocs("CONVERTIBLE"), Is.EqualTo("adr/0001-first.md:3: Packs stay convertible."));
        Assert.That(_tools.SearchDocs("unicorn"), Does.Contain("unicorn").And.Contain("ListDocs"));
        Assert.That(_tools.SearchDocs(" "), Does.Contain("needs some text"));
    }

    /// <summary>
    /// F126.5.1: a search goes through the docs in the order of their paths as it shows them, with <c>/</c>, so it
    /// answers the same on Windows and Linux. Sorted by the system's full path, Windows' <c>\</c> put <c>aA.md</c>
    /// before <c>a/x.md</c> and Linux's <c>/</c> after it, and a replay recorded on one stopped on the other.
    /// </summary>
    [Test]
    public void ASearch_GoesInTheOrderOfItsPathsWithSlashes_OnEverySystem()
    {
        Directory.CreateDirectory(Path.Combine(_root, "a"));
        File.WriteAllText(Path.Combine(_root, "a", "x.md"), "Payments are planned.\n");
        File.WriteAllText(Path.Combine(_root, "aA.md"), "Payments are planned.\n");

        Assert.That(_tools.SearchDocs("payments"),
            Is.EqualTo("a/x.md:1: Payments are planned.\naA.md:1: Payments are planned."));
    }
}
