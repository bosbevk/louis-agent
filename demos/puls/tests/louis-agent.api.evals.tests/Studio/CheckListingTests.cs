using LouisAgent.Api.Evals.Studio;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Readers.Listing;

namespace LouisAgent.Api.Evals.Tests.Evals.Studio;

/// <summary>
/// The runner's <c>--checks</c> lines read into the studio's catalogue: each check with its kind and its settings'
/// schema, each reader with its kind. One line that is neither, or a kind the API doesn't know, refuses the whole
/// listing with a reason that quotes it, as <c>--list</c>'s does.
/// </summary>
public class CheckListingTests
{
    /// <summary>Two checks and a reader, as the runner writes them.</summary>
    private const string Lines = """
                                 {"type":"within-time","kind":"fixed-rule","parameters":{"type":["object","null"],"properties":{"seconds":{"type":"integer"}},"required":["seconds"]}}
                                 {"type":"judge","kind":"model","parameters":{"type":["object","null"]}}
                                 {"reader":"rand-total","kind":"money"}
                                 """;

    /// <summary>The checks and the readers are read, each with its kind and the check's schema whole.</summary>
    [Test]
    public void TheChecksAndReaders_AreRead()
    {
        var result = CheckListing.Parse(Lines);

        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Checks.Select(check => (check.Type, check.Kind)),
            Is.EqualTo(new[] { ("within-time", CheckKind.FixedRule), ("judge", CheckKind.Model) }));
        Assert.That(result.Checks[0].Parameters.GetProperty("required")[0].GetString(), Is.EqualTo("seconds"));
        Assert.That(result.Readers.Single(), Is.EqualTo(new ReaderLine("rand-total", ReaderKind.Money)));
    }

    /// <summary>
    /// A line that is no check or reader, or not JSON, or of a kind we don't know, refuses the listing.
    /// </summary>
    [TestCase("""{"kind":"model","parameters":{}}""")]
    [TestCase("not json")]
    [TestCase("""{"type":"x","kind":"magic","parameters":{}}""")]
    [TestCase("""{"reader":"x","kind":"colour"}""")]
    [TestCase("""{"type":"x","kind":"model"}""")]
    public void ALineThatIsNoCheckOrReader_RefusesTheListing(string line)
    {
        var result = CheckListing.Parse(Lines + "\n" + line);

        Assert.That((result.Checks, result.Readers), Is.EqualTo((Array.Empty<CheckTypeLine>(),
            Array.Empty<ReaderLine>())));
        Assert.That(result.Failure, Does.Contain(line));
    }

    /// <summary>A runner that isn't there gives nothing, and says where it looked.</summary>
    [Test]
    public void AMissingRunner_GivesNothing_AndSaysWhere() =>
        Assert.That(CheckListing.ForRunner("/nope/louis-agent.evals.runner.dll").Read().Failure,
            Does.Contain("isn't at /nope/louis-agent.evals.runner.dll"));
}
