using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Reports;

namespace LouisAgent.Api.Tests.Benchmarks.Reports;

/// <summary>
/// Each eval's manual lives beside the eval (evals/{project}/Manuals/{name}.md) and opens from its card. The API
/// reads it from the path the catalogue keeps, never from one the caller names, and nothing outside evals/ can be
/// read that way.
/// </summary>
public class BenchmarkManualTests
{
    /// <summary>The repo's root, five folders up from where the tests run.</summary>
    private static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    /// <summary>Each real eval's manual is found beside the eval and opens with a heading.</summary>
    [TestCase("stub", "evals/louis-agent.evals.runner/Manuals/stub.md")]
    [TestCase("puls-cook", "evals/louis-agent.evals.puls/Manuals/cook.md")]
    [TestCase("puls-routing", "evals/louis-agent.evals.puls/Manuals/routing.md")]
    public void AnEvalsManual_IsReadFromBesideTheEval(string eval, string path)
    {
        var check = new BenchmarkCheck(eval, "An eval", path);

        Assert.That(check.ManualPath, Is.EqualTo(path));
        Assert.That(BenchmarkManuals.Read(RepoRoot, check), Does.StartWith("# "), path);
        Assert.That(BenchmarkManuals.Has(RepoRoot, check), Is.True);
    }

    /// <summary>An eval with no manual, or with a path to a file that isn't there, has none.</summary>
    [Test]
    public void AnEvalWithoutAManual_HasNone()
    {
        Assert.That(new BenchmarkCheck("ghost", "Ghost").ManualPath, Is.Null);
        Assert.That(BenchmarkManuals.Read(RepoRoot, new BenchmarkCheck("ghost", "Ghost")), Is.Null);
        Assert.That(
            BenchmarkManuals.Read(RepoRoot, new BenchmarkCheck("ghost", "Ghost", "evals/ghost/Manuals/ghost.md")),
            Is.Null, "a path that names no file");
    }

    /// <summary>A path that leads outside evals/, even by climbing out of it, is never read.</summary>
    [TestCase("evals/../config/prices.json")]
    [TestCase("../outside/manual.md")]
    [TestCase("docs/README.md")]
    [TestCase("benchmarks/stub/HANDLEIDING.md")]
    [TestCase("evals/../docs/README.md")]
    public void AManualOutsideEvals_IsNeverRead(string path)
    {
        var check = new BenchmarkCheck("escape", "Escape", path);

        Assert.That(BenchmarkManuals.Read(RepoRoot, check), Is.Null, path);
    }
}
