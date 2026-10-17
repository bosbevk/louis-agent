using LouisAgent.Api.Services;
using LouisAgent.Api.Services.Benchmarks;

namespace LouisAgent.Api.Tests;

// F31 step 10b: each check's manual (benchmarks/{check}/HANDLEIDING.md) opens from its card. The API reads it from the
// catalogue entry, never from a path the caller names, and nothing outside benchmarks/ can be read that way.
public class BenchmarkManualTests
{
    private static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void EveryCheckThePortalOffers_HasItsManual()
    {
        foreach (var check in BenchmarkCatalog.Default)
            Assert.That(BenchmarkManuals.Read(RepoRoot, check), Does.StartWith("# ").And.Contain(check.Id), check.Id);
    }

    [Test]
    public void AManual_IsBesideItsScript()
    {
        Assert.That(BenchmarkCatalog.Cook.ManualPath, Is.EqualTo("benchmarks/cook/HANDLEIDING.md"));
    }

    [Test]
    public void ACheckWithoutAManual_HasNone()
    {
        var check = new BenchmarkCheck("ghost", "Spook", "benchmarks/ghost/run_check.py");

        Assert.That(BenchmarkManuals.Read(RepoRoot, check), Is.Null);
    }

    [TestCase("benchmarks/../config/run_check.py")]
    [TestCase("../outside/run_check.py")]
    [TestCase("docs/run_check.py")]
    public void AManualOutsideBenchmarks_IsNeverRead(string script)
    {
        var check = new BenchmarkCheck("escape", "Ontsnap", script);

        Assert.That(BenchmarkManuals.Read(RepoRoot, check), Is.Null, script);
    }
}
