using LouisAgent.Core.Tools.Workspace;

namespace LouisAgent.Core.Tests.Tools.Workspace;

// The files toolset's WorkspaceFileTools (S, CL15, from WorkspaceToolsTests): a file's content in and out: read, write, append; the boundary and the secret-file checks.
public class WorkspaceFileToolsTests
{
    [Test]
    public void WriteAndReadFile_UsesWorkspaceRelativePath()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceFileTools(root);
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
            var tools = new WorkspaceFileTools(root);

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
    public void ReadFile_AllowsAnEnvTemplate()
    {
        // FX12: the delegation check couldn't answer "which settings in config/.env.example are about the LLM?".
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "config"));
        File.WriteAllText(Path.Combine(root, "config", ".env.example"), "LLM_PROFILE=anthropic");
        File.WriteAllText(Path.Combine(root, "config", ".env"), "SECRET=value");

        try
        {
            var tools = new WorkspaceFileTools(root);

            Assert.That(tools.ReadWorkspaceFile("config/.env.example"), Does.Contain("LLM_PROFILE=anthropic"));
            Assert.That(tools.ReadWorkspaceFile("config/.env"), Does.Contain("not allowed"), "the real file stays blocked");
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
            var tools = new WorkspaceFileTools(root);

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
    public void WriteAndReadFile_CreatesIntermediateDirectories()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceFileTools(root);
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
            var tools = new WorkspaceFileTools(root);
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
            var tools = new WorkspaceFileTools(root);
            string result = tools.ReadWorkspaceFile("nonexistent.txt");

            Assert.That(result, Does.Contain("error").Or.Contain("Error").Or.Contain("not found"));
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
            var tools = new WorkspaceFileTools(root);
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
    public void AppendToFile_AppendsContent()
    {
        string root = Path.Combine(Path.GetTempPath(), $"louis-agent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        try
        {
            var tools = new WorkspaceFileTools(root);
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
            var tools = new WorkspaceFileTools(root);

            tools.AppendToFile("newfile.txt", "content");

            Assert.That(File.Exists(Path.Combine(root, "newfile.txt")), Is.True);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
