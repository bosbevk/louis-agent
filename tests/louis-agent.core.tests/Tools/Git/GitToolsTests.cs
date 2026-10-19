using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.Git;

namespace LouisAgent.Core.Tests.Tools.Git;

// FX19: every git call takes a list, the model's values come after --end-of-options (paths after --), a value that
// starts with "-" is refused before git runs, and SetConfig sets only user.name and user.email. Run on real git in a
// repository made for each test, with a bare repository beside it as origin; skipped when git isn't installed. The
// happy paths pin each command's exact form: --end-of-options needs git 2.24, and a command that refuses it would
// show here, not in a chat.
public class GitToolsTests
{
    private string _root = null!;
    private string _repo = null!;
    private string _origin = null!;
    private GitTools _git = null!;

    [SetUp]
    public void SetUp()
    {
        _root = Path.Combine(Path.GetTempPath(), "git-tools-" + Guid.NewGuid().ToString("N"));
        _repo = Path.Combine(_root, "repo");
        _origin = Path.Combine(_root, "origin.git");
        Directory.CreateDirectory(_repo);
        Directory.CreateDirectory(_origin);
        if (Git(_repo, "init", "-q", "-b", "main").ExitCode != 0) Assert.Ignore("git isn't installed");
        Git(_repo, "config", "user.email", "test@example.com");
        Git(_repo, "config", "user.name", "Test");
        Git(_repo, "config", "commit.gpgsign", "false");
        // Git for Windows sets core.autocrlf globally; the file contents compared here must come back as written.
        Git(_repo, "config", "core.autocrlf", "false");
        Git(_origin, "init", "-q", "--bare", "-b", "main");
        Commit("readme.md", "Hallo\n", "Begin die projek");
        _git = new GitTools(_repo);
    }

    [TearDown]
    public void TearDown()
    {
        // git makes its object files read-only; clear that so the folder can go.
        foreach (string file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories)) File.SetAttributes(file, FileAttributes.Normal);
        Directory.Delete(_root, recursive: true);
    }

    private static ProcessRunner.Result Git(string repo, params string[] args) => ProcessRunner.Run("git", args, repo, TimeSpan.FromSeconds(30));

    private string Out(params string[] args) => Git(_repo, args).Output.Trim();

    private void Commit(string file, string content, string message)
    {
        File.WriteAllText(Path.Combine(_repo, file), content);
        Git(_repo, "add", "--", file);
        Git(_repo, "commit", "-q", "-m", message);
    }

    private string Outside(string name) => Path.Combine(_root, name);

    // ---- The doors: a value that starts with "-" never reaches git as an option ----

    [Test]
    public void ShowCommit_RefusesAValueThatIsAnOption_AndWritesNoFile()
    {
        string file = Outside("written-by-show.txt");

        string result = _git.ShowCommit($"--output={file}");

        Assert.That(result, Does.Contain("starts with '-'").And.Contain("commitRef"));
        Assert.That(File.Exists(file), Is.False, "git show --output wrote a file outside the repository");
    }

    [Test]
    public void SetConfig_SetsOnlyTheUsersNameAndEmail()
    {
        string refused = _git.SetConfig("core.fsmonitor", "touch " + Outside("fsmonitor.txt"));

        Assert.That(refused, Does.Contain("user.name").And.Contain("user.email").And.Contain("core.fsmonitor"));
        Assert.That(Git(_repo, "config", "--get", "core.fsmonitor").ExitCode, Is.Not.Zero, "the key was set");
        Assert.That(_git.SetConfig("user.name", "Anna"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.GetConfig("user.name"), Is.EqualTo("Anna"));
        Assert.That(_git.SetConfig("user.email", "anna@example.com"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.GetConfig("user.email"), Is.EqualTo("anna@example.com"));
    }

    [Test]
    public void AValueThatStartsWithADash_IsRefusedBeforeGitRuns()
    {
        string file = Outside("touched.txt");

        Assert.That(_git.Fetch($"--upload-pack=touch {file}"), Does.Contain("starts with '-'").And.Contain("remote"));
        Assert.That(_git.CreateBranch("--orphan"), Does.Contain("starts with '-'").And.Contain("branchName"));
        Assert.That(_git.SwitchBranch("-"), Does.Contain("starts with '-'"));
        Assert.That(_git.AddRemote("-x", "https://example.com/r.git"), Does.Contain("starts with '-'").And.Contain("name"));
        Assert.That(_git.AddRemote("x", "--mirror=fetch"), Does.Contain("starts with '-'").And.Contain("url"));
        Assert.That(_git.ResetHard("--soft"), Does.Contain("starts with '-'"));
        Assert.That(_git.GetConfig("--global"), Does.Contain("starts with '-'").And.Contain("key"));
        Assert.That(_git.DiffUnstaged("--output=" + file), Does.Contain("starts with '-'").And.Contain("filePath"));
        Assert.That(File.Exists(file), Is.False);
        Assert.That(_git.GetCurrentBranch(), Is.EqualTo("main"), "nothing ran");
    }

    [Test]
    public void AValueWithSpaces_ArrivesAsOneArgument()
    {
        string file = Outside("split.txt");

        string result = _git.CreateBranch($"x --upload-pack=touch {file}");

        Assert.That(result, Does.Contain("not a valid branch name"), "git saw one name, with the spaces in it");
        Assert.That(File.Exists(file), Is.False);
    }

    [Test]
    public void FreeText_ArrivesWhole_WhateverItStartsWith()
    {
        const string message = "-m \"quoted\" --and dashes; and a 'tail'";
        File.WriteAllText(Path.Combine(_repo, "a.txt"), "a\n");
        _git.Stage(".");

        Assert.That(_git.Commit(message), Does.Not.Contain("Exit Code"));
        Assert.That(Out("log", "-1", "--format=%B"), Is.EqualTo(message));

        _git.CreateBranch("topic");
        Commit("b.txt", "b\n", "On topic");
        _git.SwitchBranch("main");
        Commit("c.txt", "c\n", "On main");
        Assert.That(_git.Merge("topic", message), Does.Not.Contain("Exit Code"));
        Assert.That(Out("log", "-1", "--format=%B"), Is.EqualTo(message));

        Assert.That(_git.CreateTag("v1", message), Does.Not.Contain("Exit Code"));
        Assert.That(Out("tag", "-l", "--format=%(contents)", "v1"), Is.EqualTo(message));

        Assert.That(_git.SetConfig("user.name", "-Anna-"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.GetConfig("user.name"), Is.EqualTo("-Anna-"));
    }

    // ---- Each command's form, pinned on real git ----

    [Test]
    public void Branches_CreateSwitchAndDelete()
    {
        Assert.That(_git.CreateBranch("feature/x"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.GetCurrentBranch(), Is.EqualTo("feature/x"));
        Assert.That(_git.SwitchBranch("main"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.GetCurrentBranch(), Is.EqualTo("main"));
        Assert.That(_git.ListBranches(), Does.Contain("feature/x").And.Contain("main"));
        Assert.That(_git.DeleteBranch("feature/x"), Does.Contain("Deleted branch"));
        Assert.That(_git.ListBranches(), Does.Not.Contain("feature/x"));
    }

    [Test]
    public void Changes_StageCommitShowDiffUnstageAndDiscard()
    {
        File.WriteAllText(Path.Combine(_repo, "readme.md"), "Hallo\nWêreld\n");
        File.WriteAllText(Path.Combine(_repo, "new.txt"), "new\n");

        Assert.That(_git.HasUncommittedChanges(), Does.Contain("readme.md"));
        Assert.That(_git.DiffUnstaged("readme.md"), Does.Contain("+Wêreld"));
        Assert.That(_git.Stage("readme.md"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.DiffStaged("readme.md"), Does.Contain("+Wêreld"));
        Assert.That(_git.DiffStaged(), Does.Contain("+Wêreld"));
        Assert.That(_git.Unstage("readme.md"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.DiffStaged(), Is.EqualTo("Success (no output)."));
        Assert.That(_git.GetStatus(), Does.Contain("?? new.txt"));
        Assert.That(_git.Stage("."), Does.Not.Contain("Exit Code"));
        Assert.That(_git.Commit("Voeg wêreld by"), Does.Contain("Voeg wêreld by"));
        Assert.That(_git.ShowCommit("HEAD"), Does.Contain("Voeg wêreld by").And.Contain("readme.md").And.Contain("new.txt"));
        Assert.That(_git.GetLog(5), Does.Contain("Voeg wêreld by").And.Contain("Begin die projek"));

        File.WriteAllText(Path.Combine(_repo, "readme.md"), "changed\n");
        Assert.That(_git.DiscardUnstaged("readme.md"), Does.Not.Contain("Exit Code"));
        Assert.That(File.ReadAllText(Path.Combine(_repo, "readme.md")), Is.EqualTo("Hallo\nWêreld\n"));

        File.WriteAllText(Path.Combine(_repo, "readme.md"), "changed\n");
        _git.Stage(".");
        Assert.That(_git.ResetHard("HEAD"), Does.Contain("HEAD is now at"));
        Assert.That(_git.HasUncommittedChanges(), Is.EqualTo("No uncommitted changes."));
        Assert.That(_git.AmendCommit(), Does.Contain("Voeg wêreld by"));
    }

    [Test]
    public void Stashes_PushListApplyAndPop()
    {
        File.WriteAllText(Path.Combine(_repo, "readme.md"), "stashed\n");

        Assert.That(_git.Stash("my work"), Does.Contain("my work"));
        Assert.That(File.ReadAllText(Path.Combine(_repo, "readme.md")), Is.EqualTo("Hallo\n"));
        Assert.That(_git.ListStashes(), Does.Contain("stash@{0}").And.Contain("my work"));
        Assert.That(_git.ApplyStash("stash@{0}"), Does.Not.Contain("Exit Code"));
        Assert.That(File.ReadAllText(Path.Combine(_repo, "readme.md")), Is.EqualTo("stashed\n"));
        Assert.That(_git.ListStashes(), Does.Contain("my work"), "apply keeps it");
        _git.DiscardUnstaged(".");
        Assert.That(_git.ApplyStash("stash@{0}", pop: true), Does.Not.Contain("Exit Code"));
        Assert.That(_git.ListStashes(), Is.EqualTo("Success (no output)."), "pop removes it");

        File.WriteAllText(Path.Combine(_repo, "readme.md"), "again\n");
        Assert.That(_git.Stash(), Does.Contain("Saved working directory"));
    }

    [Test]
    public void Tags_CreateListAndDelete()
    {
        Assert.That(_git.CreateTag("light"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.CreateTag("v1.0", "First release"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.ListTags(), Does.Contain("light").And.Contain("v1.0"));
        Assert.That(Out("tag", "-l", "--format=%(contents)", "v1.0"), Is.EqualTo("First release"));
        Assert.That(_git.DeleteTag("light"), Does.Contain("Deleted tag"));
        Assert.That(_git.ListTags(), Does.Not.Contain("light"));
    }

    [Test]
    public void Remotes_AddGetUrlPushFetchAndPull()
    {
        Assert.That(_git.AddRemote("origin", _origin), Does.Not.Contain("Exit Code"));
        Assert.That(_git.GetRemoteUrl("origin"), Is.EqualTo(_origin));
        Assert.That(_git.ListRemotes(), Does.Contain("origin"));
        Assert.That(_git.GetRepositoryInfo(), Does.Contain("Current Branch: main").And.Contain("origin"));

        Assert.That(_git.Push("origin", "main"), Does.Not.Contain("Exit Code"));
        Assert.That(Git(_origin, "log", "--oneline", "main").Output, Does.Contain("Begin die projek"));

        Git(_repo, "branch", "--set-upstream-to=origin/main", "main");
        Commit("readme.md", "Hallo\nPushed\n", "Second");
        Assert.That(_git.Push(), Does.Not.Contain("Exit Code"), "defaults: origin, the current branch");
        Assert.That(_git.Fetch("origin"), Does.Not.Contain("Exit Code"));
        Assert.That(_git.Pull("origin", "main"), Does.Not.Contain("Exit Code"));
        Assert.That(Git(_origin, "log", "--oneline", "main").Output, Does.Contain("Second"));
    }

    [Test]
    public void History_MergeRebaseAndCherryPick()
    {
        _git.CreateBranch("topic");
        Commit("t.txt", "t\n", "On topic");
        _git.SwitchBranch("main");
        Commit("m.txt", "m\n", "On main");

        Assert.That(_git.Merge("topic", "Merge topic"), Does.Not.Contain("Exit Code"));
        Assert.That(Out("log", "-1", "--format=%s"), Is.EqualTo("Merge topic"));

        _git.CreateBranch("topic2");
        Commit("t2.txt", "t2\n", "On topic2");
        _git.SwitchBranch("main");
        Commit("m2.txt", "m2\n", "On main 2");
        _git.SwitchBranch("topic2");
        Assert.That(_git.Rebase("main"), Does.Not.Contain("Exit Code"));
        Assert.That(Out("log", "--oneline", "-3"), Does.Contain("On topic2").And.Contain("On main 2"));

        string topic2 = Out("rev-parse", "HEAD");
        _git.SwitchBranch("main");
        Assert.That(_git.CherryPick(topic2), Does.Not.Contain("Exit Code"));
        Assert.That(File.Exists(Path.Combine(_repo, "t2.txt")), Is.True);
        Assert.That(_git.Rebase("main", abort: true), Does.Contain("Exit Code"), "no rebase in progress: git says so, nothing else runs");
    }
}
