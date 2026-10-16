using LouisAgent.Api.Services;

namespace LouisAgent.Api.Tests;

// F27 step 1 (F27-S3): the checks the portal can run. CL10: none until they're rewritten; no saved runs on disk.
public class BenchmarkCatalogTests
{
    [Test]
    public void TheCatalog_HasNoChecks_UntilTheyAreRewritten()
    {
        // CL10: the old checks went with samples/; the rewritten ones (benchmarks/, results in puls_benchmarks) come later.
        Assert.That(new BenchmarkCatalog().Checks, Is.Empty);
    }

    [Test]
    public void ACatalog_OffersTheChecksItIsGiven()
    {
        var check = new BenchmarkCheck("cook", "Die kokkontrole", "benchmarks/cook/run_check.py", ["--x"]);

        Assert.That(new BenchmarkCatalog([check]).Checks, Is.EqualTo(new[] { check }));
    }
}
