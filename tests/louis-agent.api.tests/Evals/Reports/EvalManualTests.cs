using LouisAgent.Api.Evals.List;
using LouisAgent.Api.Evals.Reports;

namespace LouisAgent.Api.Tests.Evals.Reports;

/// <summary>
/// Each eval's manual lives beside the eval (evals/{project}/Manuals/{name}.md) and opens from its card. The API
/// reads it from the path the catalogue keeps, never from one the caller names, and nothing outside evals/ can be
/// read that way.
/// </summary>
public class EvalManualTests
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
        var check = new EvalEntry(eval, "An eval", path);

        Assert.That(check.ManualPath, Is.EqualTo(path));
        Assert.That(EvalManuals.Read(RepoRoot, check), Does.StartWith("# "), path);
        Assert.That(EvalManuals.Has(RepoRoot, check), Is.True);
    }

    /// <summary>An eval with no manual, or with a path to a file that isn't there, has none.</summary>
    [Test]
    public void AnEvalWithoutAManual_HasNone()
    {
        Assert.That(new EvalEntry("ghost", "Ghost").ManualPath, Is.Null);
        Assert.That(EvalManuals.Read(RepoRoot, new EvalEntry("ghost", "Ghost")), Is.Null);
        Assert.That(
            EvalManuals.Read(RepoRoot, new EvalEntry("ghost", "Ghost", "evals/ghost/Manuals/ghost.md")),
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
        var check = new EvalEntry("escape", "Escape", path);

        Assert.That(EvalManuals.Read(RepoRoot, check), Is.Null, path);
    }
}
