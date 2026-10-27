using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Reports;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

// F31 step 10b, moved in F18 step 7.1: each eval's manual lives beside the eval (evals/{project}/Manuals/{name}.md) and opens from its card. The runner's listing gives each eval's path
// (relative to the repo root), the catalogue entry keeps it, and the API reads it from there, never from a path the caller names. Nothing outside evals/ can be read that way.
public class BenchmarkManualTests
{
    private static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [TestCase("stub", "evals/louis-agent.evals.runner/Manuals/stub.md")]
    [TestCase("puls-cook", "evals/louis-agent.evals.puls/Manuals/cook.md")]
    [TestCase("puls-routing", "evals/louis-agent.evals.puls/Manuals/routing.md")]
    public void AnEvalsManual_IsReadFromBesideTheEval(string eval, string path)
    {
        var check = BenchmarkCheck.ForEval(eval, "An eval", path);

        Assert.That(check.ManualPath, Is.EqualTo(path));
        Assert.That(BenchmarkManuals.Read(RepoRoot, check), Does.StartWith("# "), path);
        Assert.That(BenchmarkManuals.Has(RepoRoot, check), Is.True);
    }

    [Test]
    public void AnEvalWithoutAManual_HasNone()
    {
        Assert.That(BenchmarkCheck.ForEval("ghost", "Ghost").ManualPath, Is.Null);
        Assert.That(BenchmarkManuals.Read(RepoRoot, BenchmarkCheck.ForEval("ghost", "Ghost")), Is.Null);
        Assert.That(BenchmarkManuals.Read(RepoRoot, BenchmarkCheck.ForEval("ghost", "Ghost", "evals/ghost/Manuals/ghost.md")), Is.Null, "a path that names no file");
    }

    [TestCase("evals/../config/prices.json")]
    [TestCase("../outside/manual.md")]
    [TestCase("docs/README.md")]
    [TestCase("benchmarks/stub/HANDLEIDING.md")]
    [TestCase("evals/../docs/README.md")]
    public void AManualOutsideEvals_IsNeverRead(string path)
    {
        var check = BenchmarkCheck.ForEval("escape", "Escape", path);

        Assert.That(BenchmarkManuals.Read(RepoRoot, check), Is.Null, path);
    }
}
