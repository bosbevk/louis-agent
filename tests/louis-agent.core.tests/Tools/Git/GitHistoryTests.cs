using LouisAgent.Testing;
using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.Git;
using LouisAgent.Core.Tools.Git.Running;

namespace LouisAgent.Core.Tests.Tools.Git;

// F22 step 4: the historian's tools on a small repository made for the test. Skipped when git isn't installed.
[Category("Integration")]
public class GitHistoryTests
{
    private string _repo = null!;
    private GitHistoryTools _history = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Path.Combine(Path.GetTempPath(), "git-history-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_repo);
        Outside.Tool(Git("init", "-q").ExitCode == 0, "git isn't installed");
        Git("config", "user.email", "test@example.com");
        Git("config", "user.name", "Test");
        Git("config", "commit.gpgsign", "false");

        Commit("readme.md", "Hallo\n", "Begin die projek", "Anna", "2026-09-01T10:00:00+02:00");
        Commit("readme.md", "Hallo\nWêreld\n", "Voeg wêreld by", "Ben", "2026-10-01T10:00:00+02:00");
        Commit("other.txt", "x\n", "Ander lêer", "Anna", "2026-10-05T10:00:00+02:00");
        var git = new GitRunner(_repo);
        _history = new GitHistoryTools(git);
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
        string history = _history.FileHistory("readme.md");

        Assert.That(history, Does.Contain("Voeg wêreld by").And.Contain("Begin die projek").And.Not.Contain("Ander lêer"));
        Assert.That(history.IndexOf("Voeg wêreld by", StringComparison.Ordinal), Is.LessThan(history.IndexOf("Begin die projek", StringComparison.Ordinal)));
        Assert.That(history, Does.Contain("2026-10-01").And.Contain("Ben"));
    }

    [Test]
    public void Blame_SaysWhoWroteEachLine()
    {
        string blame = _history.Blame("readme.md");

        Assert.That(blame, Does.Contain("Anna").And.Contain("Hallo"));
        Assert.That(blame, Does.Contain("Ben").And.Contain("Wêreld"));
        Assert.That(_history.Blame("readme.md", 2, 2), Does.Contain("Wêreld").And.Not.Contain("Hallo"));
    }

    [Test]
    public void SearchCommits_ByText_Author_AndDate()
    {
        Assert.That(_history.SearchCommits(text: "WÊRELD"), Does.Contain("Voeg wêreld by").And.Not.Contain("Begin"), "case-insensitive");
        Assert.That(_history.SearchCommits(author: "anna"), Does.Contain("Begin die projek").And.Contain("Ander lêer").And.Not.Contain("Voeg"));
        Assert.That(_history.SearchCommits(since: "2026-10-03"), Does.Contain("Ander lêer").And.Not.Contain("Voeg"));
        Assert.That(_history.SearchCommits(text: "niks hiervan"), Is.EqualTo("No commits match."));
        Assert.That(_history.SearchCommits(), Does.Contain("at least one of"));
    }

    [Test]
    public void ModelText_IsNeverAnOption()
    {
        // "--output=..." as a path or a search term stays a value: no file is written, no flag is set.
        _history.FileHistory("--output=hacked.txt");
        _history.SearchCommits(text: "x --output=hacked.txt");

        Assert.That(File.Exists(Path.Combine(_repo, "hacked.txt")), Is.False);
    }
}
