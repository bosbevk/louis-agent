using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;

namespace LouisAgent.Api.Tests.Benchmarks.Runs;

// F18 step 4.3: the catalogue's evals come from the runner's own --list, read once, as an argument list with a timeout. Written first:
// the listing threw NotImplementedException on the red run. The runner's real output is the pin: the API's line record must read it.
public class EvalListingTests
{
    [Test]
    public void AMissingRunnerIsAWarningAndNoEvals()
    {
        var result = EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll").Read();

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("isn't at /nope/louis-agent.evals.runner.dll"));
    }

    [Test]
    public void ANonJsonLineIsAWarningAndNoEvals()
    {
        var result = EvalListing.Parse("a line that isn't JSON");

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("isn't a JSON eval"));
    }

    // System.Text.Json fills missing parameters with defaults unless told not to: these lines must still be refused.
    [TestCase("{}")]
    [TestCase("""{"name":null,"identity":null,"version":1,"proves":"x"}""")]
    [TestCase("""{"name":"stub","identity":null,"version":null,"proves":"x"}""")]
    [TestCase("""{"name":"stub","identity":null,"version":1}""")]
    public void ALineThatIsNotAnEvalIsRefused(string line)
    {
        var result = EvalListing.Parse(line);

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("isn't a JSON eval"));
    }

    [Test]
    public void AListedEvalLineIsRead()
    {
        var result = EvalListing.Parse(SampleIdentity.Fill("""{"name":"stub","identity":"<IdentityAlpha>","version":2,"proves":"A canned run"}"""));

        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Evals.Single(), Is.EqualTo(new EvalLine("stub", 2, "A canned run", SampleIdentity.Alpha)));
    }

    [Test]
    public void AListedEvalsManualPathIsRead_AndIsNullWhenTheLineLeavesItOut()
    {
        var with = EvalListing.Parse("""{"name":"stub","identity":null,"version":1,"proves":"A canned run","manual":"evals/louis-agent.evals.runner/Manuals/stub.md"}""");
        var without = EvalListing.Parse("""{"name":"stub","version":1,"proves":"A canned run"}""");

        Assert.That(with.Evals.Single().Manual, Is.EqualTo("evals/louis-agent.evals.runner/Manuals/stub.md"));
        Assert.That(without.Evals.Single().Manual, Is.Null);
    }

    [Test]
    public void AListedEvalWithoutAnIdentityIsRead()
    {
        // The runner writes identity: null today; a runner that leaves a null out must not have its whole listing refused.
        var result = EvalListing.Parse("""{"name":"stub","version":1,"proves":"A canned run"}""");

        Assert.That(result.Failure, Is.Null);
        Assert.That((result.Evals.Single().Name, result.Evals.Single().Identity), Is.EqualTo(("stub", (string?)null)));
    }

    [Test]
    public void TheLastLineOfStderrIsTheReason()
    {
        var result = ExitingWith(2, "first line", "last line").Read();

        Assert.That(result.Failure, Is.EqualTo("the eval runner's --list exited with 2: last line"));
    }

    [Test]
    public void ANonZeroExitWithNoStderrIsOnlyTheCode()
    {
        var result = ExitingWith(3).Read();

        Assert.That(result.Failure, Is.EqualTo("the eval runner's --list exited with 3"));
    }

    /// <summary>A process that writes each line to stderr and exits with the code: the platform's shell, so the test needs no runner.</summary>
    private static EvalListing ExitingWith(int code, params string[] stderr)
    {
        var timeout = TimeSpan.FromSeconds(30);
        if (OperatingSystem.IsWindows())
            return new EvalListing("cmd", ["/c", string.Concat(stderr.Select(line => $"echo {line} 1>&2 & ")) + $"exit {code}"], timeout);
        return new EvalListing("sh", ["-c", string.Concat(stderr.Select(line => $"echo '{line}' >&2; ")) + $"exit {code}"], timeout);
    }
}
