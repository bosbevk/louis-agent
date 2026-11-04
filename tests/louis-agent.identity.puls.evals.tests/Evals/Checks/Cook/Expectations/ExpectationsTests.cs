using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Expectations;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Expectations;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Expectations;

/// <summary>
/// What a scenario asks for is read from its <c>expected</c> once, and checked there: the route, the town, the people
/// it serves and a budget. A bad value is refused by its key, naming the scenario. The shop the user chose is the
/// shops pack's to read (its <c>ScenarioShopTests</c>).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public class ExpectationsTests
{
    /// <summary>A cook scenario in Malmesbury that expects what this JSON says.</summary>
    private static EvalScenario Scenario(string expected) =>
        new("cook-x", new EvalInput("Malmesbury, Wes-Kaap, Suid-Afrika", "Africa/Johannesburg", ["x"]),
            JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev);

    /// <summary>Every key of a full expectation is read into its field.</summary>
    [Test]
    public void AWholeExpectationIsRead()
    {
        var expected = CookExpectations.Of(Scenario(
            """{"routes_to":"puls-cook","town":"Stellenbosch","serves":[4,6],"budget":250,"shop":"checkers"}"""));

        Assert.That((expected.RoutesTo, expected.Town, expected.Budget),
            Is.EqualTo((PulsEvalName.Cook.Wire(), "Stellenbosch", (decimal?)250m)));
        Assert.That(expected.Serves, Is.EqualTo(new[] { 4, 6 }));
    }

    /// <summary>The people can be one number for the first turn, one per turn, or not said at all.</summary>
    [Test]
    public void ServesIsOneNumberForTheFirstTurn_OrOnePerTurn()
    {
        Assert.That(CookExpectations.Of(Scenario("""{"routes_to":null,"serves":2}""")).Serves, Is.EqualTo(new[] { 2 }));
        Assert.That(CookExpectations.Of(Scenario("""{"routes_to":null}""")).Serves, Is.Empty);
    }

    /// <summary>A route of null means Puls should answer itself.</summary>
    [Test]
    public void ARouteOfNull_IsPulsAnsweringItself() =>
        Assert.That(ExpectedRoute.Of(Scenario("""{"routes_to":null}""")), Is.Null);

    /// <summary>A key that's missing or of the wrong kind is refused, naming the key and the scenario.</summary>
    [TestCase("""{}""", "routes_to")]
    [TestCase("""{"routes_to":5}""", "routes_to")]
    [TestCase("""{"routes_to":null,"town":7}""", "town")]
    [TestCase("""{"routes_to":null,"town":""}""", "town")]
    [TestCase("""{"routes_to":null,"budget":"veel"}""", "budget")]
    [TestCase("""{"routes_to":null,"budget":-5}""", "budget")]
    [TestCase("""{"routes_to":null,"serves":"vier"}""", "serves")]
    [TestCase("""{"routes_to":null,"serves":[4,0]}""", "serves")]
    public void AnExpectationThatIsMissingOrOfTheWrongKind_IsRefusedByItsKey(string expected, string key) =>
        Assert.That(() => CookExpectations.Of(Scenario(expected)),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains($"'{key}'").And.Message.Contains("'cook-x'"));
}
