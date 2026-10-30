using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Tests.Grading.Common;

/// <summary>
/// The harness's general checks can be named from a file: each is in the catalogue under its type, and builds the
/// same check, with today's name and version, that an eval builds in C#.
/// </summary>
public class GeneralChecksTests
{
    /// <summary>The catalogue of the general checks lists each by its type, all fixed rules.</summary>
    [Test]
    public void TheCatalogue_ListsEachGeneralCheck()
    {
        var listing = new CheckCatalog(GeneralChecks.All).Listing();

        Assert.That(listing.Select(line => line.Type),
            Is.EqualTo(new[] { "no-error", "within-time", "cost-under", "contains", "tool-used" }));
        Assert.That(listing.Select(line => line.Kind), Has.All.EqualTo(CheckKind.FixedRule));
    }

    /// <summary>Each type, given its settings, builds today's check under today's name and version.</summary>
    [TestCase("no-error", "{}", typeof(NoErrorCheck))]
    [TestCase("within-time", """{"seconds": 120}""", typeof(WithinTimeCheck))]
    [TestCase("cost-under", """{"limit_usd": 0.50}""", typeof(CostUnderCheck))]
    public void EachType_BuildsTodaysCheck(string type, string settings, Type check)
    {
        var built = Build(type, settings);

        Assert.That(built, Is.TypeOf(check));
        Assert.That(built.Name, Is.EqualTo(type));
        Assert.That(built.Version, Is.EqualTo(1));
        Assert.That(built.Required, Is.True);
    }

    /// <summary>A check named in a file can be made optional, as one built in C# can.</summary>
    [TestCase("no-error", """{"required": false}""")]
    [TestCase("within-time", """{"seconds": 120, "required": false}""")]
    [TestCase("cost-under", """{"limit_usd": 0.50, "required": false}""")]
    public void EachType_CanBeMadeOptional(string type, string settings)
    {
        Assert.That(Build(type, settings).Required, Is.False);
    }

    /// <summary>A limit left out is refused, never read as zero, naming the check type and the field.</summary>
    [TestCase("within-time", "seconds")]
    [TestCase("cost-under", "limit_usd")]
    public void AMissingLimit_IsRefused(string type, string field)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(type, "{}"));

        Assert.That(refusal!.Message, Does.Contain(type).And.Contain(field));
    }

    /// <summary>Builds a check the way an eval file will: by type, from its settings.</summary>
    private static ICheck Build(string type, string settings) =>
        new CheckCatalog(GeneralChecks.All).Get(type).Create(JsonDocument.Parse(settings).RootElement.Clone());
}
