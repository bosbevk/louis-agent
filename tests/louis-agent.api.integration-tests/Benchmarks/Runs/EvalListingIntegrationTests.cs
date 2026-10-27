using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.IntegrationTests.Benchmarks.Runs;

// F18 step 4.3: the catalogue's evals come from the runner's own --list, read once, as an argument list with a timeout. Written first:
// the listing threw NotImplementedException on the red run. The runner's real output is the pin: the API's line record must read it.
[Category("Integration")]
public class EvalListingIntegrationTests
{
    [Test]
    public void TheRunnersOwnListingReadsAsEvalLines()
    {
        // F18 step 5.3: the stub, and Puls's two evals (the runner loads their datasets from its own output folder).
        var result = EvalListing.ForRunner(RunnerBuild.Dll).Read();

        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Evals.Select(e => e.Name).ToList(), Has.Count.EqualTo(3).And.Contain("stub"),
            "the stub and the two Puls evals; the Puls eval set's tests hold their names");
        var stub = result.Evals.Single(e => e.Name == "stub");
        Assert.That(stub.Identity, Is.Null);
        Assert.That(stub.Version, Is.EqualTo(1));
        Assert.That(stub.Proves, Is.Not.Empty);
        // The Puls evals are listed under their set's identity (its name is held by the eval set's tests, which see the identity); here only that all of them have the one.
        Assert.That(result.Evals.Where(e => e.Name != "stub").Select(e => e.Identity).Distinct().ToList(),
            Has.Count.EqualTo(1).And.None.Null);
    }

    [Test]
    public void ATimedOutListingIsAWarningAndNoEvals()
    {
        var result = EvalListing.ForRunner(RunnerBuild.Dll, TimeSpan.FromMilliseconds(1)).Read();

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("didn't finish within"));
    }

    [Test]
    public void AFailingListingIsAWarningAndNoEvals()
    {
        // The runner refuses an unknown eval and exits 1: the same non-zero exit a listing would give.
        var result = new EvalListing(EvalListing.DotNetHost, [RunnerBuild.Dll, "run", "nope"], TimeSpan.FromSeconds(30))
            .Read();

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("exited with 1"));
        Assert.That(result.Failure, Does.Contain("nope"),
            "the warning carries the runner's reason, which names the eval");
    }
}
