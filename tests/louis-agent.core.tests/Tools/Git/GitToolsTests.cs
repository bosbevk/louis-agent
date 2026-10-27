using LouisAgent.Core.Tools;
using LouisAgent.Core.Tools.Git;
using LouisAgent.Core.Tools.Git.Running;

namespace LouisAgent.Core.Tests.Tools.Git;

// FX19: every git call takes a list, the model's values come after --end-of-options (paths after --), a value that
// starts with "-" is refused before git runs, and SetConfig sets only user.name and user.email. Run on real git in a
// repository made for each test, with a bare repository beside it as origin; skipped when git isn't installed. The
// happy paths pin each command's exact form: --end-of-options needs git 2.24, and a command that refuses it would
// show here, not in a chat.
[Category("Integration")]
public class GitToolsTests
{
    private string _root = null!;
    private string _repo = null!;
    private string _origin = null!;
    private GitChangeTools _changes = null!;
    private GitBranchTools _branches = null!;
    private GitRemoteTools _remotes = null!;
    private GitHistoryTools _history = null!;
    private GitConfigTools _config = null!;

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
        var git = new GitRunner(_repo);
        _changes = new GitChangeTools(git);
        _branches = new GitBranchTools(git);
        _remotes = new GitRemoteTools(git);
        _history = new GitHistoryTools(git);
        _config = new GitConfigTools(git);
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

        string result = _history.ShowCommit($"--output={file}");

        Assert.That(result, Does.Contain("starts with '-'").And.Contain("commitRef"));
        Assert.That(File.Exists(file), Is.False, "git show --output wrote a file outside the repository");
    }

    [Test]
    public void SetConfig_SetsOnlyTheUsersNameAndEmail()
    {
        string refused = _config.SetConfig("core.fsmonitor", "touch " + Outside("fsmonitor.txt"));

        Assert.That(refused, Does.Contain("user.name").And.Contain("user.email").And.Contain("core.fsmonitor"));
        Assert.That(Git(_repo, "config", "--get", "core.fsmonitor").ExitCode, Is.Not.Zero, "the key was set");
        Assert.That(_config.SetConfig("user.name", "Anna"), Does.Not.Contain("Exit Code"));
        Assert.That(_config.GetConfig("user.name"), Is.EqualTo("Anna"));
        Assert.That(_config.SetConfig("user.email", "anna@example.com"), Does.Not.Contain("Exit Code"));
        Assert.That(_config.GetConfig("user.email"), Is.EqualTo("anna@example.com"));
    }

    [Test]
    public void AValueThatStartsWithADash_IsRefusedBeforeGitRuns()
    {
        string file = Outside("touched.txt");

        Assert.That(_remotes.Fetch($"--upload-pack=touch {file}"), Does.Contain("starts with '-'").And.Contain("remote"));
        Assert.That(_branches.CreateBranch("--orphan"), Does.Contain("starts with '-'").And.Contain("branchName"));
        Assert.That(_branches.SwitchBranch("-"), Does.Contain("starts with '-'"));
        Assert.That(_remotes.AddRemote("-x", "https://example.com/r.git"), Does.Contain("starts with '-'").And.Contain("name"));
        Assert.That(_remotes.AddRemote("x", "--mirror=fetch"), Does.Contain("starts with '-'").And.Contain("url"));
        Assert.That(_changes.ResetHard("--soft"), Does.Contain("starts with '-'"));
        Assert.That(_config.GetConfig("--global"), Does.Contain("starts with '-'").And.Contain("key"));
        Assert.That(_changes.DiffUnstaged("--output=" + file), Does.Contain("starts with '-'").And.Contain("filePath"));
        Assert.That(File.Exists(file), Is.False);
        Assert.That(_branches.GetCurrentBranch(), Is.EqualTo("main"), "nothing ran");
    }

    // Review of PR #66: a refspec isn't an option, so neither guard saw it. "+main" force-pushed with force: false,
    // and ":main" deleted main on the remote; both got around the tool's own force switch.
    [Test]
    public void Push_RefusesARefspec_ThatWouldForceOrDelete()
    {
        _remotes.AddRemote("origin", _origin);
        Commit("readme.md", "Hallo\nMore\n", "Second");
        _remotes.Push("origin", "main");
        Git(_repo, "reset", "-q", "--hard", "HEAD~1");
        Commit("readme.md", "Hallo\nOther\n", "Diverged");

        Assert.That(_remotes.Push("origin", "+main"), Does.Contain("refspec").And.Contain("branch"), "a forced push, with force: false");
        Assert.That(Git(_origin, "log", "-1", "--format=%s", "main").Output.Trim(), Is.EqualTo("Second"), "origin's main was overwritten");
        Assert.That(_remotes.Push("origin", ":main"), Does.Contain("refspec").And.Contain("branch"), "a delete");
        Assert.That(Git(_origin, "rev-parse", "--verify", "refs/heads/main").ExitCode, Is.Zero, "origin's main was deleted");
        Assert.That(_remotes.Push("origin", "main:other"), Does.Contain("refspec"), "a push to another ref");
        Assert.That(_remotes.Pull("origin", "+main"), Does.Contain("refspec"), "the same value on pull");
    }

    [Test]
    public void AValueWithSpaces_ArrivesAsOneArgument()
    {
        string file = Outside("split.txt");

        string result = _branches.CreateBranch($"x --upload-pack=touch {file}");

        Assert.That(result, Does.Contain("not a valid branch name"), "git saw one name, with the spaces in it");
        Assert.That(File.Exists(file), Is.False);
    }

    [Test]
    public void FreeText_ArrivesWhole_WhateverItStartsWith()
    {
        const string message = "-m \"quoted\" --and dashes; and a 'tail'";
        File.WriteAllText(Path.Combine(_repo, "a.txt"), "a\n");
        _changes.Stage(".");

        Assert.That(_changes.Commit(message), Does.Not.Contain("Exit Code"));
        Assert.That(Out("log", "-1", "--format=%B"), Is.EqualTo(message));

        _branches.CreateBranch("topic");
        Commit("b.txt", "b\n", "On topic");
        _branches.SwitchBranch("main");
        Commit("c.txt", "c\n", "On main");
        Assert.That(_branches.Merge("topic", message), Does.Not.Contain("Exit Code"));
        Assert.That(Out("log", "-1", "--format=%B"), Is.EqualTo(message));

        Assert.That(_branches.CreateTag("v1", message), Does.Not.Contain("Exit Code"));
        Assert.That(Out("tag", "-l", "--format=%(contents)", "v1"), Is.EqualTo(message));

        Assert.That(_config.SetConfig("user.name", "-Anna-"), Does.Not.Contain("Exit Code"));
        Assert.That(_config.GetConfig("user.name"), Is.EqualTo("-Anna-"));
    }

    // ---- Each command's form, pinned on real git ----

    [Test]
    public void Branches_CreateSwitchAndDelete()
    {
        Assert.That(_branches.CreateBranch("feature/x"), Does.Not.Contain("Exit Code"));
        Assert.That(_branches.GetCurrentBranch(), Is.EqualTo("feature/x"));
        Assert.That(_branches.SwitchBranch("main"), Does.Not.Contain("Exit Code"));
        Assert.That(_branches.GetCurrentBranch(), Is.EqualTo("main"));
        Assert.That(_branches.ListBranches(), Does.Contain("feature/x").And.Contain("main"));
        Assert.That(_branches.DeleteBranch("feature/x"), Does.Contain("Deleted branch"));
        Assert.That(_branches.ListBranches(), Does.Not.Contain("feature/x"));
    }

    [Test]
    public void Changes_StageCommitShowDiffUnstageAndDiscard()
    {
        File.WriteAllText(Path.Combine(_repo, "readme.md"), "Hallo\nWêreld\n");
        File.WriteAllText(Path.Combine(_repo, "new.txt"), "new\n");

        Assert.That(_changes.HasUncommittedChanges(), Does.Contain("readme.md"));
        Assert.That(_changes.DiffUnstaged("readme.md"), Does.Contain("+Wêreld"));
        Assert.That(_changes.Stage("readme.md"), Does.Not.Contain("Exit Code"));
        Assert.That(_changes.DiffStaged("readme.md"), Does.Contain("+Wêreld"));
        Assert.That(_changes.DiffStaged(), Does.Contain("+Wêreld"));
        Assert.That(_changes.Unstage("readme.md"), Does.Not.Contain("Exit Code"));
        Assert.That(_changes.DiffStaged(), Is.EqualTo("Success (no output)."));
        Assert.That(_changes.GetStatus(), Does.Contain("?? new.txt"));
        Assert.That(_changes.Stage("."), Does.Not.Contain("Exit Code"));
        Assert.That(_changes.Commit("Voeg wêreld by"), Does.Contain("Voeg wêreld by"));
        Assert.That(_history.ShowCommit("HEAD"), Does.Contain("Voeg wêreld by").And.Contain("readme.md").And.Contain("new.txt"));
        Assert.That(_history.GetLog(5), Does.Contain("Voeg wêreld by").And.Contain("Begin die projek"));

        File.WriteAllText(Path.Combine(_repo, "readme.md"), "changed\n");
        Assert.That(_changes.DiscardUnstaged("readme.md"), Does.Not.Contain("Exit Code"));
        Assert.That(File.ReadAllText(Path.Combine(_repo, "readme.md")), Is.EqualTo("Hallo\nWêreld\n"));

        File.WriteAllText(Path.Combine(_repo, "readme.md"), "changed\n");
        _changes.Stage(".");
        Assert.That(_changes.ResetHard("HEAD"), Does.Contain("HEAD is now at"));
        Assert.That(_changes.HasUncommittedChanges(), Is.EqualTo("No uncommitted changes."));
        Assert.That(_changes.AmendCommit(), Does.Contain("Voeg wêreld by"));
    }

    [Test]
    public void Stashes_PushListApplyAndPop()
    {
        File.WriteAllText(Path.Combine(_repo, "readme.md"), "stashed\n");

        Assert.That(_changes.Stash("my work"), Does.Contain("my work"));
        Assert.That(File.ReadAllText(Path.Combine(_repo, "readme.md")), Is.EqualTo("Hallo\n"));
        Assert.That(_changes.ListStashes(), Does.Contain("stash@{0}").And.Contain("my work"));
        Assert.That(_changes.ApplyStash("stash@{0}"), Does.Not.Contain("Exit Code"));
        Assert.That(File.ReadAllText(Path.Combine(_repo, "readme.md")), Is.EqualTo("stashed\n"));
        Assert.That(_changes.ListStashes(), Does.Contain("my work"), "apply keeps it");
        _changes.DiscardUnstaged(".");
        Assert.That(_changes.ApplyStash("stash@{0}", pop: true), Does.Not.Contain("Exit Code"));
        Assert.That(_changes.ListStashes(), Is.EqualTo("Success (no output)."), "pop removes it");

        File.WriteAllText(Path.Combine(_repo, "readme.md"), "again\n");
        Assert.That(_changes.Stash(), Does.Contain("Saved working directory"));
    }

    [Test]
    public void Tags_CreateListAndDelete()
    {
        Assert.That(_branches.CreateTag("light"), Does.Not.Contain("Exit Code"));
        Assert.That(_branches.CreateTag("v1.0", "First release"), Does.Not.Contain("Exit Code"));
        Assert.That(_branches.ListTags(), Does.Contain("light").And.Contain("v1.0"));
        Assert.That(Out("tag", "-l", "--format=%(contents)", "v1.0"), Is.EqualTo("First release"));
        Assert.That(_branches.DeleteTag("light"), Does.Contain("Deleted tag"));
        Assert.That(_branches.ListTags(), Does.Not.Contain("light"));
    }

    [Test]
    public void Remotes_AddGetUrlPushFetchAndPull()
    {
        Assert.That(_remotes.AddRemote("origin", _origin), Does.Not.Contain("Exit Code"));
        Assert.That(_remotes.GetRemoteUrl("origin"), Is.EqualTo(_origin));
        Assert.That(_remotes.ListRemotes(), Does.Contain("origin"));
        Assert.That(_history.GetRepositoryInfo(), Does.Contain("Current Branch: main").And.Contain("origin"));

        Assert.That(_remotes.Push("origin", "main"), Does.Not.Contain("Exit Code"));
        Assert.That(Git(_origin, "log", "--oneline", "main").Output, Does.Contain("Begin die projek"));

        Git(_repo, "branch", "--set-upstream-to=origin/main", "main");
        Commit("readme.md", "Hallo\nPushed\n", "Second");
        Assert.That(_remotes.Push(), Does.Not.Contain("Exit Code"), "defaults: origin, the current branch");
        Assert.That(_remotes.Fetch("origin"), Does.Not.Contain("Exit Code"));
        Assert.That(_remotes.Pull("origin", "main"), Does.Not.Contain("Exit Code"));
        Assert.That(Git(_origin, "log", "--oneline", "main").Output, Does.Contain("Second"));
    }

    [Test]
    public void History_MergeRebaseAndCherryPick()
    {
        _branches.CreateBranch("topic");
        Commit("t.txt", "t\n", "On topic");
        _branches.SwitchBranch("main");
        Commit("m.txt", "m\n", "On main");

        Assert.That(_branches.Merge("topic", "Merge topic"), Does.Not.Contain("Exit Code"));
        Assert.That(Out("log", "-1", "--format=%s"), Is.EqualTo("Merge topic"));

        _branches.CreateBranch("topic2");
        Commit("t2.txt", "t2\n", "On topic2");
        _branches.SwitchBranch("main");
        Commit("m2.txt", "m2\n", "On main 2");
        _branches.SwitchBranch("topic2");
        Assert.That(_branches.Rebase("main"), Does.Not.Contain("Exit Code"));
        Assert.That(Out("log", "--oneline", "-3"), Does.Contain("On topic2").And.Contain("On main 2"));

        string topic2 = Out("rev-parse", "HEAD");
        _branches.SwitchBranch("main");
        Assert.That(_branches.CherryPick(topic2), Does.Not.Contain("Exit Code"));
        Assert.That(File.Exists(Path.Combine(_repo, "t2.txt")), Is.True);
        Assert.That(_branches.Rebase("main", abort: true), Does.Contain("Exit Code"), "no rebase in progress: git says so, nothing else runs");
    }
}
