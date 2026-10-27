using LouisAgent.Choices;
using LouisAgent.Evals.Puls.Shared;
using System.Text.Json;
using LouisAgent.Evals.Datasets;
using LouisAgent.Evals.Puls.Evals.Cook.Expectations;
using LouisAgent.Evals.Puls.Shared.Expectations;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Expectations;

// F18 step 5.4: what a scenario asks for, read from its `expected` once and checked there (ADR 0024): a route, the town, the people it serves, a budget
// and the shop the user chose. Written first: the readers threw NotImplementedException on the red run.
public class ExpectationsTests
{
    private static EvalScenario Scenario(string expected) =>
        new("cook-x", new EvalInput("Malmesbury, Wes-Kaap, Suid-Afrika", "Africa/Johannesburg", ["x"]),
            JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev);

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

    [Test]
    public void ServesIsOneNumberForTheFirstTurn_OrOnePerTurn()
    {
        Assert.That(CookExpectations.Of(Scenario("""{"routes_to":null,"serves":2}""")).Serves, Is.EqualTo(new[] { 2 }));
        Assert.That(CookExpectations.Of(Scenario("""{"routes_to":null}""")).Serves, Is.Empty);
    }

    [Test]
    public void ARouteOfNull_IsPulsAnsweringItself() =>
        Assert.That(ExpectedRoute.Of(Scenario("""{"routes_to":null}""")), Is.Null);

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

    [Test]
    public void AShopAtATown_NeedsItsTown()
    {
        // The cook's prompt: area = the user's town in every call. A scenario that expects a shop without its town couldn't be graded.
        Assert.That(() => CookExpectations.Of(Scenario("""{"routes_to":"puls-cook","shop":"checkers"}""")),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'town'"));
        Assert.That(CookExpectations.Of(Scenario("""{"routes_to":"puls-cook","shop":"any"}""")).Shop,
            Is.EqualTo(ShopExpectation.Any), "no shop is searched, so no town is needed");
    }

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
