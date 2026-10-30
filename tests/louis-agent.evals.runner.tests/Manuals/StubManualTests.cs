using LouisAgent.Evals.Runner.Evals.Stub;

namespace LouisAgent.Evals.Runner.Tests.Manuals;

// F18 step 7.1: the stub's manual lives beside the stub (evals/louis-agent.evals.runner/Manuals/stub.md), in English, and names the eval, its modes and every grader the stub declares, in backticks,
// so a new grader can't ship unexplained (the Python test_manuals.py did this for scores; the graders are what the C# evals declare). Written first: the manual was
// benchmarks/stub/HANDLEIDING.md, about the Python.
public class StubManualTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    private static string FindRepoRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory is not null;
             directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "louis-agent-solution.sln")))
                return directory.FullName;
        throw new InvalidOperationException("the solution's folder was not found above " + AppContext.BaseDirectory);
    }

    private static string Manual(IEval eval) =>
        File.ReadAllText(Path.Combine(RepoRoot, eval.Manual!.Replace('/', Path.DirectorySeparatorChar)));

    [Test]
    public void TheStubListsItsManual_BesideItself()
    {
        Assert.That(new StubEval().Manual, Is.EqualTo("evals/louis-agent.evals.runner/Manuals/stub.md"));
    }

    [Test]
    public async Task TheManual_OpensWithATitle_AndNamesTheEvalItsModesAndEveryGrader()
    {
        var eval = new StubEval();
        string text = Manual(eval);
        var modes = await eval.ModesAsync(null!, CancellationToken.None);

        Assert.That(text, Does.StartWith("# "));
        Assert.That(text, Does.Contain($"`{eval.Name}`"));
        Assert.That(modes.Select(m => m.Name), Has.All.Matches<string>(mode => text.Contains($"`{mode}`")),
            "every mode, in backticks");
        Assert.That(eval.Checks.Select(g => g.Name), Has.All.Matches<string>(grader => text.Contains($"`{grader}`")),
            "every grader, in backticks");
    }
}
