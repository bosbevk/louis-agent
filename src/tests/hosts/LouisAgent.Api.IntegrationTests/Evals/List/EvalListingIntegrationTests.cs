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
    /// <summary>
    /// The test runner's own listing reads as the stub and the template's file eval, each with its graders.
    /// </summary>
    [Test]
    public void TheRunnersOwnListingReadsAsEvalLines()
    {
        // The stub, and the framework's template, the one file eval the test runner copies beside it (F166.1); the
        // runner loads its dataset from its own output folder.
        var result = EvalListing.ForRunner(RunnerBuild.Dll).Read();

        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Evals.Select(e => e.Name), Is.EquivalentTo(new[] { "stub", "template-example" }));
        var stub = result.Evals.Single(e => e.Name == "stub");
        var template = result.Evals.Single(e => e.Name == "template-example");
        Assert.That(stub.Identity, Is.Null);
        Assert.That(stub.Version, Is.EqualTo(1));
        Assert.That(stub.Proves, Is.Not.Empty);
        // A file eval is listed under the identity it proves, with its manual where it sits in the repository.
        Assert.That((template.Identity, template.Manual),
            Is.EqualTo(("template", "src/evals/template/Example/manual.md")));
        // Every eval lists its graders, a fixed rule among them, and a file eval its dataset; the stub has none.
        Assert.That(result.Evals.Select(e => e.Graders?.Any(g => g.Kind == CheckKind.FixedRule)),
            Has.All.True);
        Assert.That(stub.Dataset, Is.Null);
        Assert.That(template.Dataset, Is.Not.Null);
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
