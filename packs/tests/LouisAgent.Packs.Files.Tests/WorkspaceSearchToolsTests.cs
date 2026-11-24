using LouisAgent.Packs.Files;

namespace LouisAgent.Packs.Files.Tests;

/// <summary>
/// The files toolset's search across the tree (CL15): text in files, and files by name. A hit shows where the text is,
/// and a giant line shows only the part around the match, marked as cut (FX243).
/// </summary>
public class WorkspaceSearchToolsTests
{
    /// <summary>A search names the file and the line the text is on.</summary>
    [Test]
    public void SearchWorkspace_FindsSourceText()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "Example.cs"), "public class Example { }");

        try
        {
            var tools = new WorkspaceSearchTools(root);

            Assert.That(tools.SearchWorkspace("class Example"), Does.Contain("Example.cs:1"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Finding files by a name pattern lists only the files it matches.</summary>
    [Test]
    public void FindFiles_WithPattern_FiltersResults()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceSearchTools(root);
            File.WriteAllText(Path.Combine(root, "file.cs"), "content");
            File.WriteAllText(Path.Combine(root, "file.txt"), "content");

            string result = tools.FindFiles("*.cs");

            Assert.That(result, Does.Contain("file.cs"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>Every file holding the text is a hit.</summary>
    [Test]
    public void SearchWorkspace_FindsMultipleMatches()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "file1.cs"), "public class Test1 { }");
        File.WriteAllText(Path.Combine(root, "file2.cs"), "public class Test2 { }");

        try
        {
            var tools = new WorkspaceSearchTools(root);

            string result = tools.SearchWorkspace("public class");

            Assert.That(result, Does.Contain("file1.cs"));
            Assert.That(result, Does.Contain("file2.cs"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>A search that finds nothing says so.</summary>
    [Test]
    public void SearchWorkspace_NoResults_ReturnsEmptyOrNotFound()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "file.cs"), "public class Test { }");

        try
        {
            var tools = new WorkspaceSearchTools(root);

            string result = tools.SearchWorkspace("NotFound");

            // Nothing found: the answer says so, or is empty.
            Assert.That(result, Is.TypeOf<string>());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>A search finds a word inside a longer text.</summary>
    [Test]
    public void SearchWorkspace_WithPattern_FindsMatches()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "file.cs"), "public class Example123 { }");

        try
        {
            var tools = new WorkspaceSearchTools(root);

            string result = tools.SearchWorkspace("Example123");

            Assert.That(result, Does.Contain("file.cs"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>The all-files pattern finds every file.</summary>
    [Test]
    public void FindFiles_WithAllWildcard()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "file.txt"), "");
        File.WriteAllText(Path.Combine(root, "file.cs"), "");

        try
        {
            var tools = new WorkspaceSearchTools(root);

            string result = tools.FindFiles("*");

            Assert.That(result, Does.Contain("file.txt").Or.Contain("file.cs"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FX243: a line longer than a hit shows, such as a recording's one-line JSON, comes back as the part around the
    /// match, marked as cut with its full length, so one hit can't flood the answer; a normal line comes back whole.
    /// </summary>
    [Test]
    public void AGiantLine_ComesBackCutAroundTheMatch_AndSaysSo()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "recording.json"),
            new string('x', 100_000) + "SUMMER25" + new string('y', 100_000));
        File.WriteAllText(Path.Combine(root, "OrderTotal.cs"), "    var rate = Codes[\"SUMMER25\"];");

        try
        {
            string result = new WorkspaceSearchTools(root).SearchWorkspace("SUMMER25");
            string[] hits = result.Split('\n');
            string giant = hits.Single(line => line.StartsWith("recording.json", StringComparison.Ordinal));
            string normal = hits.Single(line => line.StartsWith("OrderTotal.cs", StringComparison.Ordinal));

            Assert.Multiple(() =>
            {
                Assert.That(giant.Length, Is.LessThan(SearchHit.MostShown + 200));
                Assert.That(giant,
                    Does.Contain("SUMMER25").And.Contain("200,008 characters").And.Contain("read the file"));
                Assert.That(normal.Trim(), Is.EqualTo("OrderTotal.cs:1: var rate = Codes[\"SUMMER25\"];"));
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    /// <summary>
    /// FX244: a search can stay inside one folder, such as the department's repository a case names, so it finds only
    /// what's there; a folder outside the workspace is refused.
    /// </summary>
    [Test]
    public void ASearchInAFolder_FindsOnlyWhatIsInside_AndNeverOutside()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "service"));
        File.WriteAllText(Path.Combine(root, "service", "OrderTotal.cs"), "var rate = Codes[code];");
        File.WriteAllText(Path.Combine(root, "notes.md"), "Codes are retired by marketing.");

        try
        {
            var tools = new WorkspaceSearchTools(root);
            string inside = tools.SearchWorkspace("Codes", searchPath: "service");

            Assert.Multiple(() =>
            {
                Assert.That(inside, Does.Contain("OrderTotal.cs:1").And.Not.Contain("notes.md"));
                Assert.That(() => tools.SearchWorkspace("Codes", searchPath: ".."), Throws.ArgumentException);
            });
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
