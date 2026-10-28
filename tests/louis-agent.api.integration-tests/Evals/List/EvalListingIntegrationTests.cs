using LouisAgent.Api.Evals.List;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Api.IntegrationTests.Evals.List;

/// <summary>
/// Runs the real eval runner's <c>--list</c> and checks the API can read every line it writes. Listing asks no model;
/// it only describes the evals.
/// </summary>
[Category("Integration")]
public class EvalListingIntegrationTests
{
    /// <summary>The runner's own listing reads as the stub and Puls's two evals, each with its graders.</summary>
    [Test]
    public void TheRunnersOwnListingReadsAsEvalLines()
    {
        // The stub, and Puls's two evals (the runner loads their datasets from its own output folder).
        var result = EvalListing.ForRunner(RunnerBuild.Dll).Read();

        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Evals.Select(e => e.Name).ToList(), Has.Count.EqualTo(3).And.Contain("stub"),
            "the stub and the two Puls evals; the Puls eval set's tests hold their names");
        var stub = result.Evals.Single(e => e.Name == "stub");
        Assert.That(stub.Identity, Is.Null);
        Assert.That(stub.Version, Is.EqualTo(1));
        Assert.That(stub.Proves, Is.Not.Empty);
        // The Puls evals are listed under their set's identity. Its name is held by the eval set's tests, which see the
        // identity; here only that all of them have the one.
        Assert.That(result.Evals.Where(e => e.Name != "stub").Select(e => e.Identity).Distinct().ToList(),
            Has.Count.EqualTo(1).And.None.Null);
        // Every eval lists its graders, a fixed rule among them, and the Puls evals their datasets; the stub has none.
        Assert.That(result.Evals.Select(e => e.Graders?.Any(g => g.Kind == GraderKind.FixedRule)),
            Has.All.True);
        Assert.That(stub.Dataset, Is.Null);
        Assert.That(result.Evals.Where(e => e.Name != "stub").Select(e => e.Dataset), Has.None.Null);
    }

    /// <summary>A listing that takes too long gives no evals and says so.</summary>
    [Test]
    public void ATimedOutListingIsAWarningAndNoEvals()
    {
        // A millisecond is far too short for the runner to start.
        var result = EvalListing.ForRunner(RunnerBuild.Dll, TimeSpan.FromMilliseconds(1)).Read();

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("didn't finish within"));
    }

    /// <summary>A runner that exits with an error gives no evals, and its reason.</summary>
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
