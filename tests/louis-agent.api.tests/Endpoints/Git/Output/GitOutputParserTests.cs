using LouisAgent.Api.Endpoints.Git.Output;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;

namespace LouisAgent.Api.Tests.Endpoints.Git.Output;

// CL20 step 4 (ADR 0034): git's formats are pure logic, so they have their own tests. These pin what the parser did inside the
// endpoints before it moved.
public class GitOutputParserTests
{
    [Test]
    public void ParseStatus_ReadsChangesUntrackedAndRenames()
    {
        var files = GitOutputParser.ParseStatus(" M src/a.cs\0?? new file.txt\0R  b.txt\0a.txt\0");

        Assert.That(files, Is.EqualTo(new[]
        {
            new GitFileStatus("src/a.cs", null, " ", "M"),
            new GitFileStatus("new file.txt", null, "?", "?"),
            new GitFileStatus("b.txt", "a.txt", "R", " "),
        }));
        Assert.That(files[0].Unstaged, Is.True);
        Assert.That(files[0].Staged, Is.False);
        Assert.That(files[1].Untracked, Is.True);
        Assert.That(files[2].Staged, Is.True);
        Assert.That(files[2].Unstaged, Is.False);
    }

    [Test]
    public void ParseStatus_OfNothing_IsEmpty() => Assert.That(GitOutputParser.ParseStatus(""), Is.Empty);

    [Test]
    public void ParseCommits_ReadsRefsAndParents_AndDropsABareHead()
    {
        const char field = '\x1f', record = '\x1e';
        string output =
            string.Join(field, "full", "short", "Louis", "2026-10-25T09:00:00+02:00", "subject", "HEAD -> main, origin/main, tag: v1", "p1 p2") + record + "\n" +
            string.Join(field, "f2", "s2", "A", "2026-10-24T08:00:00+02:00", "s", "HEAD", "") + record + "\n";

        var commits = GitOutputParser.ParseCommits(output);

        Assert.That(commits, Has.Count.EqualTo(2));
        Assert.That(commits[0].Sha, Is.EqualTo("full"));
        Assert.That(commits[0].ShortSha, Is.EqualTo("short"));
        Assert.That(commits[0].Author, Is.EqualTo("Louis"));
        Assert.That(commits[0].Date, Is.EqualTo("2026-10-25T09:00:00+02:00"));
        Assert.That(commits[0].Subject, Is.EqualTo("subject"));
        Assert.That(commits[0].Refs, Is.EqualTo(new[] { "main", "origin/main", "tag: v1" }));
        Assert.That(commits[0].Parents, Is.EqualTo(new[] { "p1", "p2" }));
        Assert.That(commits[1].Sha, Is.EqualTo("f2"));
        Assert.That(commits[1].Refs, Is.Empty);
        Assert.That(commits[1].Parents, Is.Empty);
    }

    [Test]
    public void SafeDiff_KeepsASecretFilesLine_ButNotItsContent()
    {
        var (diff, truncated) = GitOutputParser.SafeDiff("diff --git a/src/a.cs b/src/a.cs\n+code\ndiff --git a/.env b/.env\n+SECRET=1\n");

        Assert.That(diff, Does.Contain("+code"));
        Assert.That(diff, Does.Contain("diff --git a/.env b/.env\n" + MessageText.Format(ApiUserText.SecretDiffBinary)));
        Assert.That(diff, Does.Not.Contain("SECRET"));
        Assert.That(truncated, Is.False);
    }

    [Test]
    public void SafeDiff_CapsALongPatch()
    {
        var (diff, truncated) = GitOutputParser.SafeDiff(new string('x', GitOutputParser.MaxDiffChars + 10));

        Assert.That(diff, Has.Length.EqualTo(GitOutputParser.MaxDiffChars));
        Assert.That(truncated, Is.True);
    }

    [TestCase("abc1234", true)]
    [TestCase("ABCDEF0", true)]
    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [TestCase("abc123", false)]
    [TestCase("abc123g", false)]
    [TestCase("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    public void CommitHash_IsSevenToFortyHexDigits(string text, bool expected) =>
        Assert.That(GitOutputParser.CommitHash().IsMatch(text), Is.EqualTo(expected));
}
