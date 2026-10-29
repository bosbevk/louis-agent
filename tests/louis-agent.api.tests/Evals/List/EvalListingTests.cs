using LouisAgent.Api.Evals.List;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Api.Tests.Evals.List;

/// <summary>
/// The catalogue's evals come from the runner's own <c>--list</c>: each line read into the API's record, and a listing
/// that can't be read a warning with no evals.
/// </summary>
public class EvalListingTests
{
    /// <summary>A runner that isn't there gives no evals and says where it looked.</summary>
    [Test]
    public void AMissingRunnerIsAWarningAndNoEvals()
    {
        var result = EvalListing.ForRunner("/nope/louis-agent.evals.runner.dll").Read();

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("isn't at /nope/louis-agent.evals.runner.dll"));
    }

    /// <summary>A line that isn't JSON refuses the listing.</summary>
    [Test]
    public void ANonJsonLineIsAWarningAndNoEvals()
    {
        var result = EvalListing.Parse("a line that isn't JSON");

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("isn't a JSON eval"));
    }

    /// <summary>
    /// A line missing its name, version or proof is no eval. System.Text.Json would fill in defaults unless told not
    /// to, so these must still be refused.
    /// </summary>
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

    /// <summary>A line with its name, identity, version and proof reads as that eval.</summary>
    [Test]
    public void AListedEvalLineIsRead()
    {
        var result =
            EvalListing.Parse(
                SampleIdentity.Fill(
                    """{"name":"stub","identity":"<IdentityAlpha>","version":2,"proves":"A canned run"}"""));

        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Evals.Single(), Is.EqualTo(new EvalLine("stub", 2, "A canned run", SampleIdentity.Alpha)));
    }

    /// <summary>The manual's path is read when the line has one, and is null when it doesn't.</summary>
    [Test]
    public void AListedEvalsManualPathIsRead_AndIsNullWhenTheLineLeavesItOut()
    {
        var with = EvalListing.Parse(
            """{"name":"stub","identity":null,"version":1,"proves":"A canned run","manual":"evals/"""
            + """louis-agent.evals.runner/Manuals/stub.md"}""");
        var without = EvalListing.Parse("""{"name":"stub","version":1,"proves":"A canned run"}""");

        Assert.That(with.Evals.Single().Manual, Is.EqualTo("evals/louis-agent.evals.runner/Manuals/stub.md"));
        Assert.That(without.Evals.Single().Manual, Is.Null);
    }

    /// <summary>
    /// What the portal shows before an eval ever ran is read from its line: the dataset, the categories and each
    /// grader with its kind.
    /// </summary>
    [Test]
    public void AListedEvalsDatasetCategoriesAndGradersAreRead()
    {
        var result = EvalListing.Parse(
            """{"name":"cook","version":3,"proves":"Meals","dataset":{"name":"cook-set","version":2}, """
            + """ "categories":["town","city"],"graders":[{"name":"priced","version":1,"kind":"fixed-rule"}, """
            + """ {"name":"judge","version":2,"kind":"model"}]}""");

        var cook = result.Evals.Single();
        Assert.That(cook.Dataset, Is.EqualTo(new EvalDatasetLine("cook-set", 2)));
        Assert.That(cook.Categories, Is.EqualTo(new[] { "town", "city" }));
        Assert.That(cook.Graders,
            Is.EqualTo(new[]
            {
                new EvalGraderLine("priced", 1, GraderKind.FixedRule),
                new EvalGraderLine("judge", 2, GraderKind.Model)
            }));
    }

    /// <summary>A grader whose kind the API doesn't know refuses the line, like any line that isn't an eval.</summary>
    [Test]
    public void AGraderOfAnUnknownKindIsRefused()
    {
        var result = EvalListing.Parse(
            """{"name":"cook","version":3,"proves":"Meals","graders":[{"name":"x","version":1,"kind":"Oracle"}]}""");

        Assert.That(result.Evals, Is.Empty);
        Assert.That(result.Failure, Does.Contain("isn't a JSON eval"));
    }

    /// <summary>A line that leaves the identity out still reads, as an eval of no identity.</summary>
    [Test]
    public void AListedEvalWithoutAnIdentityIsRead()
    {
        var result = EvalListing.Parse("""{"name":"stub","version":1,"proves":"A canned run"}""");

        Assert.That(result.Failure, Is.Null);
        Assert.That((result.Evals.Single().Name, result.Evals.Single().Identity), Is.EqualTo(("stub", (string?)null)));
    }

    /// <summary>When the runner refuses, its last line on stderr is the reason we give.</summary>
    [Test]
    public void TheLastLineOfStderrIsTheReason()
    {
        var result = ExitingWith(2, "first line", "last line").Read();

        Assert.That(result.Failure, Is.EqualTo("the eval runner's --list exited with 2: last line"));
    }

    /// <summary>A refusal with nothing on stderr gives only the exit code.</summary>
    [Test]
    public void ANonZeroExitWithNoStderrIsOnlyTheCode()
    {
        var result = ExitingWith(3).Read();

        Assert.That(result.Failure, Is.EqualTo("the eval runner's --list exited with 3"));
    }

    /// <summary>
    /// A process that writes each line to stderr and exits with the code. It's the platform's own shell, so the test
    /// needs no runner.
    /// </summary>
    private static EvalListing ExitingWith(int code, params string[] stderr)
    {
        var timeout = TimeSpan.FromSeconds(30);
        if (OperatingSystem.IsWindows())
            return new EvalListing("cmd",
                ["/c", string.Concat(stderr.Select(line => $"echo {line} 1>&2 & ")) + $"exit {code}"], timeout);
        return new EvalListing("sh",
            ["-c", string.Concat(stderr.Select(line => $"echo '{line}' >&2; ")) + $"exit {code}"], timeout);
    }
}
