using LouisAgent.Api.Services;

namespace LouisAgent.Api.Tests;

// F27 step 1 (F27-S3): the checks the portal can run; F31 brings them back, each with its script under benchmarks/.
public class BenchmarkCatalogTests
{
    private static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));

    [Test]
    public void AnEmptyCatalog_HasNoChecks() =>
        Assert.That(new BenchmarkCatalog().Checks, Is.Empty);

    [Test]
    public void ThePortalsChecks_AreTheStubTheCookAndTheRouting_EachWithItsScript()
    {
        // F31 steps 6c, 7b and 8.
        Assert.That(BenchmarkCatalog.Default.Select(c => c.Id), Is.EqualTo(new[] { "stub", "cook", "routing" }));
        foreach (var check in BenchmarkCatalog.Default)
            Assert.That(File.Exists(Path.Combine(RepoRoot, check.Script)), check.Script);
    }

    [Test]
    public void ACatalog_OffersTheChecksItIsGiven()
    {
        var check = new BenchmarkCheck("cook", "Die kokkontrole", "benchmarks/cook/run_check.py", ["--x"]);

        Assert.That(new BenchmarkCatalog([check]).Checks, Is.EqualTo(new[] { check }));
    }
}
