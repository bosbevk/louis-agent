using LouisAgent.Testing;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Conformance;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Runner.Catalog;
using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Template;

/// <summary>
/// The template a person copies to start an eval is a folder of files that runs with no C# at all: it loads with the
/// harness's general checks alone, its reference passes and its known-bad fails, and the runner never lists it.
/// </summary>
public class TemplateFolderTests
{
    /// <summary>Where the template sits in the repository.</summary>
    private const string Source = "evals/template/Example";

    /// <summary>The template's eval's name, as its file gives it.</summary>
    private const string Name = "template-example";

    /// <summary>It loads from its folder with only the general checks: an eval that uses them needs no C#.</summary>
    [Test]
    public void TheTemplate_LoadsWithTheGeneralChecksAlone()
    {
        var eval = Load();

        Assert.That((eval.Name, eval.Version), Is.EqualTo((Name, 1)));
        Assert.That(eval.Checks.Select(check => (check.Name, check.Required)),
            Is.EqualTo(new[] { ("no-error", true), ("within-time", false) }));
        Assert.That(eval.Scenarios, Has.Count.EqualTo(1));
        Assert.That(eval.Manual, Is.EqualTo($"{Source}/manual.md"));
    }

    /// <summary>Its reference passes its required checks and its known-bad fails them, as every eval's must.</summary>
    [Test]
    public async Task ItsReferenceAndKnownBad_Conform()
    {
        var eval = Load();

        var failures = await EvalConformance.CheckAsync(eval, ConformanceCases.Load(eval.Transcripts, eval.Scenarios),
            CancellationToken.None);

        Assert.That(failures, Is.Empty);
    }

    /// <summary>No project copies it beside the runner, so no run and no portal ever shows it.</summary>
    [Test]
    public void TheRunner_NeverListsIt()
    {
        var files = EvalFolders.Find(Path.Combine(AppContext.BaseDirectory, "EvalFiles"))
            .Select(folder => FileEval.Load(folder.Folder, RunnerChecks.Catalog, folder.Source));

        Assert.That(new EvalCatalog(files, new StubEval()).Evals.Select(eval => eval.Name), Does.Not.Contain(Name));
    }

    /// <summary>The template's eval, read from the repository with the general checks only.</summary>
    private static FileEval Load() =>
        FileEval.Load(Path.Combine(TestPaths.RepoRoot, "evals", "template", "Example"),
            new CheckCatalog(GeneralChecks.All), Source);
}
