using System.Diagnostics;
using LouisAgent.Testing;

namespace LouisAgent.Api.IntegrationTests.Endpoints.Git;

/// <summary>
/// A small git repository in the temp folder with one commit on <c>main</c>, for a test that drives the git routes.
/// It skips the test when git isn't installed, and removes itself afterwards.
/// </summary>
internal sealed class TempGitRepo : IDisposable
{
    /// <summary>Makes the repository and its first commit, <c>Begin</c>, adding <c>readme.md</c>.</summary>
    public TempGitRepo()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "git-api-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
        try
        {
            Git("init", "-q", "-b", "main");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            Outside.Tool(false, "git isn't installed");
        }

        Git("config", "user.email", "test@example.com");
        Git("config", "user.name", "Test");
        Git("config", "commit.gpgsign", "false");
        Write("readme.md", "Hallo\n");
        Git("add", "readme.md");
        Git("commit", "-q", "-m", "Begin");
    }

    /// <summary>Where the repository is.</summary>
    public string Path { get; }

    /// <summary>Writes a file in the repository.</summary>
    public void Write(string name, string text) => File.WriteAllText(System.IO.Path.Combine(Path, name), text);

    /// <summary>Runs git in the repository, with its arguments as a list, and hands back what it printed.</summary>
    public string Git(params string[] args)
    {
        var psi = new ProcessStartInfo("git")
        {
            WorkingDirectory = Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        foreach (string arg in args) psi.ArgumentList.Add(arg);
        using var process = Process.Start(psi)!;
        var error = process.StandardError.ReadToEndAsync();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {error.Result}");
        return output;
    }

    /// <summary>Removes the repository; git makes its object files read-only, so that is cleared first.</summary>
    public void Dispose()
    {
        if (!Directory.Exists(Path)) return;
        foreach (string file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
            File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(Path, recursive: true);
    }
}
