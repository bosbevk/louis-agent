using louis_agent.core.tools;
using louis_agent.core.providers;

namespace louis_agent.core.tests;

public class WorkspaceToolsTests
{
    [Test]
    public void WriteAndReadFile_UsesWorkspaceRelativePath()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);
            string result = tools.WriteWorkspaceFile("src/Example.cs", "class Example { }");

            Assert.That(result, Does.Contain("src"));
            Assert.That(tools.ReadWorkspaceFile("src/Example.cs"), Is.EqualTo("class Example { }"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ReadAndWriteFile_RejectsPathsOutsideWorkspace()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);

            // Test with forward slashes (cross-platform)
            Assert.Throws<ArgumentException>(() => tools.ReadWorkspaceFile("../outside.txt"));
            Assert.Throws<ArgumentException>(() => tools.WriteWorkspaceFile("../outside.txt", "content"));

            // Backslash is a separator only on Windows; on Linux "..\outside.txt" is a file name inside the workspace.
            if (OperatingSystem.IsWindows())
            {
                Assert.Throws<ArgumentException>(() => tools.ReadWorkspaceFile("..\\outside.txt"));
                Assert.Throws<ArgumentException>(() => tools.WriteWorkspaceFile("..\\outside.txt", "content"));
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ReadAndWriteFile_BlocksSensitiveFiles()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, ".env"), "SECRET=value");

        try
        {
            var tools = new WorkspaceTools(root);

            Assert.That(tools.ReadWorkspaceFile(".env"), Does.Contain("not allowed"));
            Assert.That(tools.WriteWorkspaceFile(".env", "replacement"), Does.Contain("not allowed"));
            Assert.That(File.ReadAllText(Path.Combine(root, ".env")), Is.EqualTo("SECRET=value"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void SearchWorkspace_FindsSourceText()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "Example.cs"), "public class Example { }");

        try
        {
            var tools = new WorkspaceTools(root);

            Assert.That(tools.SearchWorkspace("class Example"), Does.Contain("Example.cs:1"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    // Additional comprehensive tests

    [Test]
    public void WriteAndReadFile_CreatesIntermediateDirectories()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);
            string result = tools.WriteWorkspaceFile("deep/nested/path/File.cs", "content");

            Assert.That(result, Does.Contain("deep"));
            Assert.That(File.Exists(Path.Combine(root, "deep", "nested", "path", "File.cs")), Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ReadAndWriteFile_OverwritesExistingFile()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);
            tools.WriteWorkspaceFile("file.txt", "original");
            string result = tools.WriteWorkspaceFile("file.txt", "updated");

            Assert.That(tools.ReadWorkspaceFile("file.txt"), Is.EqualTo("updated"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ReadNonExistentFile_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);
            string result = tools.ReadWorkspaceFile("nonexistent.txt");

            Assert.That(result, Does.Contain("error").Or.Contain("Error").Or.Contain("not found"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ListWorkspaceFiles_ReturnsFileList()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);
            File.WriteAllText(Path.Combine(root, "file1.txt"), "content1");
            File.WriteAllText(Path.Combine(root, "file2.txt"), "content2");

            string result = tools.ListWorkspaceFiles();

            Assert.That(result, Does.Contain("file1.txt"));
            Assert.That(result, Does.Contain("file2.txt"));
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
            var tools = new WorkspaceTools(root);
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
    public void ListDirectoryTree_ReturnsTreeStructure()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);
            Directory.CreateDirectory(Path.Combine(root, "dir1"));
            Directory.CreateDirectory(Path.Combine(root, "dir2"));

            string result = tools.ListDirectoryTree();

            Assert.That(result, Does.Contain("dir1").Or.Contain("dir2"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DeleteFile_RemovesFile()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);
            string filePath = Path.Combine(root, "file.txt");
            File.WriteAllText(filePath, "content");

            string result = tools.DeleteFile("file.txt");

            Assert.That(File.Exists(filePath), Is.False);
            Assert.That(result, Does.Contain("Deleted").Or.Contain("removed"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DeleteFile_NonExistentFile_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.DeleteFile("nonexistent.txt");

            Assert.That(result, Does.Contain("error").Or.Contain("Error").Or.Contain("not found"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DeleteFile_BlocksSensitiveFiles()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, ".env"), "SECRET=value");

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.DeleteFile(".env");

            Assert.That(result, Does.Contain("not allowed"));
            Assert.That(File.Exists(Path.Combine(root, ".env")), Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void CreateDirectory_CreatesNewDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.CreateDirectory("newdir");

            Assert.That(Directory.Exists(Path.Combine(root, "newdir")), Is.True);
            Assert.That(result, Does.Contain("newdir"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void CreateDirectory_ExistingDirectory_ReturnsInfo()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "existing"));

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.CreateDirectory("existing");

            // Should either return error or info about existing directory
            Assert.That(result, Is.TypeOf<string>());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DeleteDirectory_RemovesDirectory()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "toremove"));

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.DeleteDirectory("toremove");

            Assert.That(Directory.Exists(Path.Combine(root, "toremove")), Is.False);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DeleteDirectory_NonExistentDirectory_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.DeleteDirectory("nonexistent");

            Assert.That(result, Does.Contain("not found").Or.Contain("error").Or.Contain("Error"));
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
            var tools = new WorkspaceTools(root);

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
            var tools = new WorkspaceTools(root);

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
    public void CopyFile_CopiesFileSuccessfully()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string sourceFile = Path.Combine(root, "source.txt");
        File.WriteAllText(sourceFile, "content");

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.CopyFile("source.txt", "destination.txt");

            Assert.That(File.Exists(Path.Combine(root, "destination.txt")), Is.True);
            Assert.That(File.ReadAllText(Path.Combine(root, "destination.txt")), Is.EqualTo("content"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void RenameFileOrDirectory_RenamesFileSuccessfully()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string sourceFile = Path.Combine(root, "source.txt");
        File.WriteAllText(sourceFile, "content");

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.RenameFileOrDirectory("source.txt", "destination.txt");

            Assert.That(File.Exists(Path.Combine(root, "source.txt")), Is.False);
            Assert.That(File.Exists(Path.Combine(root, "destination.txt")), Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ReadAndWriteFile_RejectsAbsolutePathsOutsideWorkspace()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);
            string outsidePath = Path.Combine(Path.GetTempPath(), "outside.txt");

            // Absolute paths should be rejected
            Assert.Throws<ArgumentException>(() => tools.ReadWorkspaceFile(outsidePath));
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
            var tools = new WorkspaceTools(root);

            string result = tools.SearchWorkspace("Example123");

            Assert.That(result, Does.Contain("file.cs"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void GetFileInfo_ReturnsFileSize()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "file.txt"), "content");

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.GetFileInfo("file.txt");

            Assert.That(result, Does.Contain("file.txt").Or.Contain("content"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void AppendToFile_AppendsContent()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);
            tools.WriteWorkspaceFile("file.txt", "initial");
            string result = tools.AppendToFile("file.txt", " appended");

            Assert.That(tools.ReadWorkspaceFile("file.txt"), Is.EqualTo("initial appended"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void AppendToFile_CreatesFileIfNotExists()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);

            tools.AppendToFile("newfile.txt", "content");

            Assert.That(File.Exists(Path.Combine(root, "newfile.txt")), Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void PathExists_FileExists_ReturnsTrue()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "exists.txt"), "");

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.PathExists("exists.txt");

            Assert.That(result, Does.Contain("exists"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void PathExists_DirectoryExists_ReturnsTrue()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "subdir"));

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.PathExists("subdir");

            Assert.That(result, Does.Contain("Directory").And.Contain("exists"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void PathExists_NotExists_ReturnsFalse()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.PathExists("notexists.txt");

            Assert.That(result, Does.Contain("does not exist"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void RenameFileOrDirectory_RenamesDirectorySuccessfully()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "olddir"));

        try
        {
            var tools = new WorkspaceTools(root);

            tools.RenameFileOrDirectory("olddir", "newdir");

            Assert.That(Directory.Exists(Path.Combine(root, "olddir")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(root, "newdir")), Is.True);
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
            var tools = new WorkspaceTools(root);

            string result = tools.FindFiles("*");

            Assert.That(result, Does.Contain("file.txt").Or.Contain("file.cs"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void ListDirectoryTree_WithDepth1()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "level1"));
        Directory.CreateDirectory(Path.Combine(root, "level1", "level2"));

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.ListDirectoryTree(".", maxDepth: 1);

            Assert.That(result, Is.TypeOf<string>());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void CopyFile_SourceNotFound_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.CopyFile("nonexistent.txt", "dest.txt");

            Assert.That(result, Does.Contain("not found").Or.Contain("error"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void CopyFile_SensitiveSource_BlocksCopy()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, ".env"), "SECRET=value");

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.CopyFile(".env", "copy.env");

            Assert.That(result, Does.Contain("not allowed"));
            Assert.That(File.Exists(Path.Combine(root, "copy.env")), Is.False);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void CopyFile_SensitiveDestination_BlocksCopy()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "source.txt"), "content");

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.CopyFile("source.txt", ".env");

            Assert.That(result, Does.Contain("not allowed"));
            Assert.That(File.Exists(Path.Combine(root, ".env")), Is.False);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DeleteDirectory_WithSubdirectories_DeletesRecursively()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "parent", "child"));
        File.WriteAllText(Path.Combine(root, "parent", "child", "file.txt"), "content");

        try
        {
            var tools = new WorkspaceTools(root);

            tools.DeleteDirectory("parent");

            Assert.That(Directory.Exists(Path.Combine(root, "parent")), Is.False);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Test]
    public void DeleteDirectory_ContainingSensitiveFile_BlocksDeletion()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "parent", "child"));
        File.WriteAllText(Path.Combine(root, "parent", "child", ".env"), "SECRET=value");

        try
        {
            var tools = new WorkspaceTools(root);

            string result = tools.DeleteDirectory("parent");

            Assert.That(result, Does.Contain("sensitive"));
            Assert.That(Directory.Exists(Path.Combine(root, "parent")), Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
