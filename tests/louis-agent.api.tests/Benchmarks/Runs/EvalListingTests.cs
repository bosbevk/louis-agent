using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F18 step 4.3: the catalogue's evals come from the runner's own --list, read once, as an argument list with a timeout. Written first:
// the listing threw NotImplementedException on the red run. The runner's real output is the pin: the API's line record must read it.
public class EvalListingTests
{
    [Test]
    public void TheRunnersOwnListingReadsAsEvalLines()
    {
        var result = EvalListing.ForRunner(RunnerBuild.Dll).Read();

        Assert.That(result.Failure, Is.Null);
        var stub = result.Evals.Single();
        Assert.That(stub.Name, Is.EqualTo("stub"));
        Assert.That(stub.Identity, Is.Null);
        Assert.That(stub.Version, Is.EqualTo(1));
        Assert.That(stub.Proves, Is.Not.Empty);
    }

    [Test]
    public void AMissingRunnerIsAWarningAndNoEvals()
    {
        var result = EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll").Read();

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("isn't at /nope/louis-agent.evals.runner.dll"));
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
        var result = new EvalListing("dotnet", [RunnerBuild.Dll, "run", "nope"], TimeSpan.FromSeconds(30)).Read();

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("exited with 1"));
    }

    [Test]
    public void ANonJsonLineIsAWarningAndNoEvals()
    {
        // `dotnet --version` exits 0 and prints a version, not a line of the listing.
        var result = new EvalListing("dotnet", ["--version"], TimeSpan.FromSeconds(30)).Read();

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("isn't JSON"));
    }
}
