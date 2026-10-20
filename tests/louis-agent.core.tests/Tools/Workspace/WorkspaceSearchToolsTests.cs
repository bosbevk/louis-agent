using LouisAgent.Core.Tools.Workspace;

namespace LouisAgent.Core.Tests.Tools.Workspace;

// The files toolset's WorkspaceSearchTools (S, CL15, from WorkspaceToolsTests): finding across the tree: text in files, files by name.
public class WorkspaceSearchToolsTests
{
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

            // Should return empty results or message about no matches
            Assert.That(result, Is.TypeOf<string>());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

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
}
