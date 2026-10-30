using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Grading.Common;
using LouisAgent.Evals.Grading.Common.Facts;
using LouisAgent.Evals.Grading.Common.Tools;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Tests.Grading.Common;

/// <summary>
/// The harness's general checks can be named from a file: each is in the catalogue under its type, and builds the
/// same check, with today's name and version, that an eval builds in C#.
/// </summary>
public class GeneralChecksTests
{
    /// <summary>
    /// The catalogue of the general checks lists each by its type: the fixed rules, then the judge and a person's
    /// review of it.
    /// </summary>
    [Test]
    public void TheCatalogue_ListsEachGeneralCheck()
    {
        var listing = new CheckCatalog(GeneralChecks.All).Listing();

        Assert.That(listing.Select(line => (line.Type, line.Kind)),
            Is.EqualTo(new[]
            {
                ("no-error", CheckKind.FixedRule), ("within-time", CheckKind.FixedRule),
                ("cost-under", CheckKind.FixedRule), ("turns-under", CheckKind.FixedRule),
                ("tokens-under", CheckKind.FixedRule), ("contains", CheckKind.FixedRule),
                ("not-contains", CheckKind.FixedRule), ("starts-with", CheckKind.FixedRule),
                ("length-under", CheckKind.FixedRule), ("mentions-expected", CheckKind.FixedRule),
                ("is-json", CheckKind.FixedRule), ("routed-to", CheckKind.FixedRule),
                ("tool-used", CheckKind.FixedRule), ("tool-not-used", CheckKind.FixedRule),
                ("tool-used-with", CheckKind.FixedRule), ("tool-calls-under", CheckKind.FixedRule),
                ("amount-under", CheckKind.FixedRule), ("number-named", CheckKind.FixedRule),
                ("image-shown", CheckKind.FixedRule), ("links-to-host", CheckKind.FixedRule),
                ("near", CheckKind.FixedRule), ("judge", CheckKind.Model), ("human", CheckKind.Human)
            }));
    }

    /// <summary>Each type, given its settings, builds today's check under today's name and version.</summary>
    [TestCase("no-error", "{}", typeof(NoErrorCheck))]
    [TestCase("within-time", """{"seconds": 120}""", typeof(WithinTimeCheck))]
    [TestCase("cost-under", """{"limit_usd": 0.50}""", typeof(CostUnderCheck))]
    [TestCase("routed-to", """{"expected_key": "routes_to", "delegating_toolsets": ["x"]}""", typeof(RoutedToCheck))]
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
    [TestCase("routed-to", """{"expected_key": "routes_to", "delegating_toolsets": ["x"], "required": false}""")]
    public void EachType_CanBeMadeOptional(string type, string settings)
    {
        Assert.That(Build(type, settings).Required, Is.False);
    }

    /// <summary>A setting left out is refused, never read as a default, naming the check type and the field.</summary>
    [TestCase("within-time", "seconds")]
    [TestCase("cost-under", "limit_usd")]
    [TestCase("routed-to", "expected_key")]
    public void AMissingLimit_IsRefused(string type, string field)
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build(type, "{}"));

        Assert.That(refusal!.Message, Does.Contain(type).And.Contain(field));
    }

    /// <summary>Builds a check the way an eval file will: by type, from its settings.</summary>
    private static ICheck Build(string type, string settings) =>
        new CheckCatalog(GeneralChecks.All).Get(type)
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
