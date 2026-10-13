using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.Git;

namespace LouisAgent.Core.Tests.Tools.Git;

// F22 step 4: the historian's tools on a small repository made for the test. Skipped when git isn't installed.
public class GitHistoryTests
{
    private string _repo = null!;
    private GitTools _git = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Path.Combine(Path.GetTempPath(), "git-history-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_repo);
        if (Git("init", "-q").ExitCode != 0) Assert.Ignore("git isn't installed");
        Git("config", "user.email", "test@example.com");
        Git("config", "user.name", "Test");
        Git("config", "commit.gpgsign", "false");

        Commit("readme.md", "Hallo\n", "Begin die projek", "Anna", "2026-09-01T10:00:00+02:00");
        Commit("readme.md", "Hallo\nWêreld\n", "Voeg wêreld by", "Ben", "2026-10-01T10:00:00+02:00");
        Commit("other.txt", "x\n", "Ander lêer", "Anna", "2026-10-05T10:00:00+02:00");
        _git = new GitTools(_repo);
    }

    [TearDown]
    public void TearDown()
    {
        // git makes its object files read-only; clear that so the folder can go.
        foreach (string file in Directory.EnumerateFiles(_repo, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_repo, recursive: true);
    }

    private ProcessRunner.Result Git(params string[] args) => ProcessRunner.Run("git", args, _repo, TimeSpan.FromSeconds(30));

    private void Commit(string file, string content, string message, string author, string date)
    {
        File.WriteAllText(Path.Combine(_repo, file), content);
        Git("add", file);
        ProcessRunner.Run("git", ["commit", "-q", "-m", message, "--author", $"{author} <{author.ToLowerInvariant()}@example.com>"], _repo,
            TimeSpan.FromSeconds(30), environment: new Dictionary<string, string> { ["GIT_AUTHOR_DATE"] = date, ["GIT_COMMITTER_DATE"] = date });
    }

    [Test]
    public void FileHistory_ListsTheCommitsThatChangedTheFile_NewestFirst()
    {
        string history = _git.FileHistory("readme.md");

        Assert.That(history, Does.Contain("Voeg wêreld by").And.Contain("Begin die projek").And.Not.Contain("Ander lêer"));
        Assert.That(history.IndexOf("Voeg wêreld by", StringComparison.Ordinal), Is.LessThan(history.IndexOf("Begin die projek", StringComparison.Ordinal)));
        Assert.That(history, Does.Contain("2026-10-01").And.Contain("Ben"));
    }

    [Test]
    public void Blame_SaysWhoWroteEachLine()
    {
        string blame = _git.Blame("readme.md");

        Assert.That(blame, Does.Contain("Anna").And.Contain("Hallo"));
        Assert.That(blame, Does.Contain("Ben").And.Contain("Wêreld"));
        Assert.That(_git.Blame("readme.md", 2, 2), Does.Contain("Wêreld").And.Not.Contain("Hallo"));
    }

    [Test]
    public void SearchCommits_ByText_Author_AndDate()
    {
        Assert.That(_git.SearchCommits(text: "WÊRELD"), Does.Contain("Voeg wêreld by").And.Not.Contain("Begin"), "case-insensitive");
        Assert.That(_git.SearchCommits(author: "anna"), Does.Contain("Begin die projek").And.Contain("Ander lêer").And.Not.Contain("Voeg"));
        Assert.That(_git.SearchCommits(since: "2026-10-03"), Does.Contain("Ander lêer").And.Not.Contain("Voeg"));
        Assert.That(_git.SearchCommits(text: "niks hiervan"), Is.EqualTo("No commits match."));
        Assert.That(_git.SearchCommits(), Does.Contain("at least one of"));
    }

    [Test]
    public void ModelText_IsNeverAnOption()
    {
        // "--output=..." as a path or a search term stays a value: no file is written, no flag is set.
        _git.FileHistory("--output=hacked.txt");
        _git.SearchCommits(text: "x --output=hacked.txt");

        Assert.That(File.Exists(Path.Combine(_repo, "hacked.txt")), Is.False);
    }
}
