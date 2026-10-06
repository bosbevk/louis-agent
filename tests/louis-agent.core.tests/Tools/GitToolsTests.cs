using louis_agent.core.tools;

namespace louis_agent.core.tests;

public class GitToolsTests
{
    private readonly string _testRepoPath;
    private readonly GitTools _gitTools;

    public GitToolsTests()
    {
        _testRepoPath = Path.Combine(Path.GetTempPath(), $"git-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRepoPath);
        _gitTools = new GitTools(_testRepoPath);
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(_testRepoPath))
        {
            Directory.Delete(_testRepoPath, recursive: true);
        }
    }

    [Test]
    public void Constructor_WithValidPath_InitializesSuccessfully()
    {
        Assert.That(_gitTools, Is.Not.Null);
    }

    [Test]
    [Ignore("Requires git command-line tool to be installed and available in PATH")]
    public void GetStatus_ReturnsString()
    {
        InitializeTestRepo();
        var result = _gitTools.GetStatus();
        Assert.That(result, Is.TypeOf<string>());
    }

    [Test]
    [Ignore("Requires git command-line tool to be installed and available in PATH")]
    public void GetCurrentBranch_ReturnsString()
    {
        InitializeTestRepo();
        var result = _gitTools.GetCurrentBranch();
        Assert.That(result, Is.Not.Empty);
        Assert.That(result, Does.Contain("master").Or.Contain("main"));
    }

    [Test]
    [Ignore("Requires git command-line tool to be installed and available in PATH")]
    public void GetLog_ReturnsCommitHistory()
    {
        InitializeTestRepo();
        CreateTestCommit("Initial commit");
        var result = _gitTools.GetLog(5);
        Assert.That(result, Is.TypeOf<string>());
    }

    [Test]
    [Ignore("Requires git command-line tool to be installed and available in PATH")]
    public void CreateBranch_ReturnsString()
    {
        InitializeTestRepo();
        var result = _gitTools.CreateBranch("feature/test");
        Assert.That(result, Is.TypeOf<string>());
    }

    [Test]
    [Ignore("Requires git command-line tool to be installed and available in PATH")]
    public void ListBranches_ReturnsString()
    {
        InitializeTestRepo();
        var result = _gitTools.ListBranches(false);
        Assert.That(result, Is.TypeOf<string>());
    }

    private void InitializeTestRepo()
    {
        RunGitCommand("init");
        RunGitCommand("config user.email \"test@example.com\"");
        RunGitCommand("config user.name \"Test User\"");
    }

    private void CreateTestCommit(string message)
    {
        File.WriteAllText(Path.Combine(_testRepoPath, "test.txt"), "test content");
        RunGitCommand("add .");
        RunGitCommand($"commit -m \"{message}\"");
    }

    private void RunGitCommand(string command)
    {
        var processInfo = new System.Diagnostics.ProcessStartInfo("git")
        {
            Arguments = command,
            WorkingDirectory = _testRepoPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(processInfo);
        process?.WaitForExit();
    }
}
