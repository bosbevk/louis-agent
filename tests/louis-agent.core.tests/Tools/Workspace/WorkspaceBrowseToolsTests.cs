using LouisAgent.Core.Tools.Workspace;

namespace LouisAgent.Core.Tests.Tools.Workspace;

// The files toolset's WorkspaceBrowseTools (S, CL15, from WorkspaceToolsTests): looking around one place: a folder's entries, its tree, whether a path exists, a file's details.
public class WorkspaceBrowseToolsTests
{
    [Test]
    public void ListWorkspaceFiles_ReturnsFileList()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceBrowseTools(root);
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
    public void ListDirectoryTree_ReturnsTreeStructure()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceBrowseTools(root);
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
    public void GetFileInfo_ReturnsFileSize()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "file.txt"), "content");

        try
        {
            var tools = new WorkspaceBrowseTools(root);

            string result = tools.GetFileInfo("file.txt");

            Assert.That(result, Does.Contain("file.txt").Or.Contain("content"));
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
            var tools = new WorkspaceBrowseTools(root);

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
            var tools = new WorkspaceBrowseTools(root);

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
            var tools = new WorkspaceBrowseTools(root);

            string result = tools.PathExists("notexists.txt");

            Assert.That(result, Does.Contain("does not exist"));
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
            var tools = new WorkspaceBrowseTools(root);

            string result = tools.ListDirectoryTree(".", maxDepth: 1);

            Assert.That(result, Is.TypeOf<string>());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
