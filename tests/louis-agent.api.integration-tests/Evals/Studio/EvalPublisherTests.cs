using LouisAgent.Api.Evals.Studio.Publish;
using LouisAgent.Api.IntegrationTests.Endpoints.Git;

namespace LouisAgent.Api.IntegrationTests.Evals.Studio;

/// <summary>
/// Publishing an eval (F49.5): its folder is committed beside its identity on a branch of its own, off the
/// workspace's HEAD, through a temporary index, so the person's working tree and branch stay exactly as they were; the
/// push and the pull request are theirs. A second publish replaces the folder on the branch, a file it dropped gone.
/// </summary>
[Category("Integration")]
public class EvalPublisherTests
{
    /// <summary>A folder as the studio writes it: the eval's file and a rubric.</summary>
    private static string Folder(params (string Path, string Text)[] files)
    {
        string folder = Path.Combine(Path.GetTempPath(), "louis-agent-publish-" + Guid.NewGuid().ToString("N")[..8]);
        foreach (var (path, text) in files)
        {
            string file = Path.Combine(folder, path);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            File.WriteAllText(file, text);
        }

        return folder;
    }

    /// <summary>The folder is on the branch, beside the identity; HEAD and the working tree are untouched.</summary>
    [Test]
    public async Task TheFolder_IsCommittedOnItsBranch_AndTheWorkingTreeIsUntouched()
    {
        using var repo = new TempGitRepo();
        string head = repo.Git("rev-parse", "HEAD").Trim();
        string folder = Folder(("eval.json", "{\"name\":\"weather-eval\"}\n"), ("Rubrics/weather-right.md", "Reg?\n"));
        var target = PublishTarget.Of("weather-agent", "weather-eval");

        var published = await new EvalPublisher(repo.Path).PublishAsync(folder, target, "Louis's weather eval");

        Assert.That(published.Branch, Is.EqualTo("eval/weather-agent-weather-eval"));
        Assert.That(repo.Git("show", $"{published.Branch}:{target.Folder}/eval.json"),
            Is.EqualTo("{\"name\":\"weather-eval\"}\n"));
        Assert.That(repo.Git("show", $"{published.Branch}:{target.Folder}/Rubrics/weather-right.md"),
            Is.EqualTo("Reg?\n"));
        Assert.That(repo.Git("rev-parse", $"{published.Branch}~1").Trim(), Is.EqualTo(head),
            "off the workspace's HEAD");
        Assert.That((repo.Git("rev-parse", "HEAD").Trim(), repo.Git("rev-parse", "--abbrev-ref", "HEAD").Trim()),
            Is.EqualTo((head, "main")), "HEAD and the branch stay");
        Assert.That(repo.Git("status", "--porcelain"), Is.Empty, "the working tree stays");
        Assert.That(repo.Git("log", "-1", "--format=%s", published.Branch).Trim(), Is.EqualTo("Louis's weather eval"));
    }

    /// <summary>Publishing again replaces the folder on the branch: a file the eval dropped is gone.</summary>
    [Test]
    public async Task PublishingAgain_ReplacesTheFolder()
    {
        using var repo = new TempGitRepo();
        var target = PublishTarget.Of("weather-agent", "weather-eval");
        var publisher = new EvalPublisher(repo.Path);
        await publisher.PublishAsync(Folder(("eval.json", "{}\n"), ("Rubrics/old.md", "Old\n")), target, "First");

        var second = await publisher.PublishAsync(Folder(("eval.json", "{\"v\":2}\n")), target, "Second");

        var files = repo.Git("ls-tree", "-r", "--name-only", second.Branch).Split('\n',
            StringSplitOptions.RemoveEmptyEntries);
        Assert.That(files, Is.EquivalentTo(new[] { "readme.md", $"{target.Folder}/eval.json" }));
    }

    /// <summary>
    /// A git that takes too long is a refusal in words, as a failing one is, never a cancellation the route lets
    /// through as a 500.
    /// </summary>
    [Test]
    public void AGitThatTakesTooLong_IsARefusalInWords()
    {
        using var repo = new TempGitRepo();
        var publisher = new EvalPublisher(repo.Path, TimeSpan.Zero);

        var refusal = Assert.ThrowsAsync<InvalidOperationException>(() =>
            publisher.PublishAsync(Folder(("eval.json", "{}\n")), PublishTarget.Of("weather-agent", "w"), "Late"));

        Assert.That(refusal!.Message, Does.Contain("git rev-parse took longer than 0 seconds"));
    }
}
