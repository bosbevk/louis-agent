using LouisAgent.Evals.Runner.Arguments;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Evals.Stub;
using LouisAgent.Evals.Runner.Studio;
using LouisAgent.Testing;

namespace LouisAgent.Evals.Runner.Tests.Studio;

/// <summary>
/// The eval a run runs (F49.5's try): one of the catalogue's by its name, or the eval in the folder <c>--folder</c>
/// names, which must be the eval the run names; and with <c>--samples</c>, only those scenarios, each one the eval has.
/// </summary>
public class RunTargetTests
{
    /// <summary>The template eval, as a studio folder would be.</summary>
    private static readonly string Template = Path.Combine(TestPaths.RepoRoot, "src", "evals", "template", "Example");

    /// <summary>The catalogue with only the stub.</summary>
    private static readonly EvalCatalog Catalogue = new([], new StubEval());

    /// <summary>A run's flags parse to the folder and the samples.</summary>
    [Test]
    public void TheFolderAndSamples_AreParsed() =>
        Assert.That(CommandLine.Parse(["run", "x", "--folder", "/tmp/a", "--samples", "one,two", "--replay", "r1"]),
            Is.EqualTo(new RunRequest("x", 1, Core.Sessions.Replay.WebReplayMode.Replay, "r1", Model: true,
                Folder: "/tmp/a", Samples: "one,two")));

    /// <summary>Samples that aren't kebab-case ids are refused, and a re-score takes neither flag.</summary>
    [TestCase("run", "x", "--samples", "One Two")]
    [TestCase("run", "x", "--rescore", "r1", "--folder", "/tmp/a")]
    public void BadSamples_OrARescoreWithAFolder_AreRefused(params string[] args) =>
        Assert.That(() => CommandLine.Parse(args), Throws.InstanceOf<RunnerException>());

    /// <summary>Without a folder, the run's eval is the catalogue's.</summary>
    [Test]
    public void WithoutAFolder_TheEvalIsTheCatalogues() =>
        Assert.That(RunTarget.Of(new RunRequest("stub", 1), Catalogue).Name, Is.EqualTo("stub"));

    /// <summary>With a folder, the eval is the folder's, which must be the one the run names.</summary>
    [Test]
    public void WithAFolder_TheEvalIsTheFolders_AndMustBeTheOneNamed()
    {
        Assert.That(RunTarget.Of(new RunRequest("template-example", 1, Folder: Template), Catalogue).Name,
            Is.EqualTo("template-example"));
        Assert.That(() => RunTarget.Of(new RunRequest("other", 1, Folder: Template), Catalogue),
            Throws.InstanceOf<RunnerException>().With.Message.Contains("template-example"));
    }

    /// <summary>With samples, only those scenarios are run, and one the eval doesn't have is refused.</summary>
    [Test]
    public void WithSamples_OnlyThoseAreRun_AndAnUnknownOneIsRefused()
    {
        var eval = RunTarget.Of(new RunRequest("template-example", 1, Folder: Template, Samples: "example"),
            Catalogue);

        Assert.That(eval.Scenarios.Select(s => s.Id), Is.EqualTo(new[] { "example" }));
        Assert.That(() => RunTarget.Of(new RunRequest("template-example", 1, Folder: Template, Samples: "nope"),
            Catalogue), Throws.InstanceOf<RunnerException>().With.Message.Contains("nope"));
    }
}
