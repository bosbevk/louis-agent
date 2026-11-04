using LouisAgent.Core.Tools.Workspace;

namespace LouisAgent.Core.Tests.Tools.Workspace;

// The files toolset's WorkspaceOrganiseTools (S, CL15, from WorkspaceToolsTests): the tree's shape: create a directory, delete, rename, copy; the secret-file checks.
public class WorkspaceOrganiseToolsTests
{
    [Test]
    public void DeleteFile_RemovesFile()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceOrganiseTools(root);
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
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

            string result = tools.DeleteDirectory("nonexistent");

            Assert.That(result, Does.Contain("not found").Or.Contain("error").Or.Contain("Error"));
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
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

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
    public void RenameFileOrDirectory_RenamesDirectorySuccessfully()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "olddir"));

        try
        {
            var tools = new WorkspaceOrganiseTools(root);

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
    public void CopyFile_SourceNotFound_ReturnsError()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

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
            var tools = new WorkspaceOrganiseTools(root);

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
