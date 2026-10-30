using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Expectations;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.Identities.Puls.Evals.Checks.Shared.Expectations;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Expectations;

/// <summary>
/// What a scenario asks for is read from its <c>expected</c> once, and checked there: the route, the town, the people
/// it serves, a budget and the shop the user chose. A bad value is refused by its key, naming the scenario.
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

        Assert.That((expected.RoutesTo, expected.Town, expected.Budget, expected.Shop),
            Is.EqualTo((PulsEvalName.Cook.Wire(), "Stellenbosch", (decimal?)250m,
                (ShopExpectation?)ShopExpectation.Checkers)));
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

    /// <summary>The shop is parsed once, by its name, whatever its case.</summary>
    [TestCase("any", ShopExpectation.Any)]
    [TestCase("Any", ShopExpectation.Any)]
    [TestCase("checkers", ShopExpectation.Checkers)]
    [TestCase("pnp", ShopExpectation.Pnp)]
    [TestCase("PnP", ShopExpectation.Pnp)]
    [TestCase("woolworths", ShopExpectation.Woolworths)]
    [TestCase("no-branch", ShopExpectation.NoBranch)]
    [TestCase("NoBranch", ShopExpectation.NoBranch)]
    public void TheShopIsParsedOnceByItsName_CaseIgnored(string name, ShopExpectation expected) =>
        Assert.That(
            CookExpectations.Of(Scenario("""{"routes_to":null,"town":"Paarl","shop":"NAME"}""".Replace("NAME", name)))
                .Shop, Is.EqualTo(expected));

    /// <summary>A scenario that expects a real shop must name the town too; "any shop" needs none.</summary>
    [Test]
    public void AShopAtATown_NeedsItsTown()
    {
        // The cook's prompt sends the user's town as the area in every call, so a scenario that expects a shop
        // without its town couldn't be graded.
        Assert.That(() => CookExpectations.Of(Scenario("""{"routes_to":"puls-cook","shop":"checkers"}""")),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'town'"));
        Assert.That(CookExpectations.Of(Scenario("""{"routes_to":"puls-cook","shop":"any"}""")).Shop,
            Is.EqualTo(ShopExpectation.Any), "no shop is searched, so no town is needed");
    }

    /// <summary>A key that's missing or of the wrong kind is refused, naming the key and the scenario.</summary>
    [TestCase("""{}""", "routes_to")]
    [TestCase("""{"routes_to":5}""", "routes_to")]
    [TestCase("""{"routes_to":null,"shop":"gremlins"}""", "shop")]
    [TestCase("""{"routes_to":null,"shop":"none"}""", "shop")]
    [TestCase("""{"routes_to":null,"shop":1}""", "shop")]
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
