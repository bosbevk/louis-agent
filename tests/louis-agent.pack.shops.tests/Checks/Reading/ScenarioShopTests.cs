using System.Text.Json;
using LouisAgent.Evals.Datasets.Scenarios;
using LouisAgent.Packs.Shops.Checks.Reading;

namespace LouisAgent.Packs.Shops.Tests.Checks.Reading;

/// <summary>
/// The shop a scenario says the user chose, and the town they shop in, read once from its <c>expected</c>: a bad value
/// is refused by its key, naming the scenario. Moved from Puls's <c>ExpectationsTests</c> with the reading (F49.3).
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public class ScenarioShopTests
{
    /// <summary>A cook scenario in Malmesbury that expects what this JSON says.</summary>
    private static EvalScenario Scenario(string expected) =>
        new("cook-x", new EvalInput("Malmesbury, Wes-Kaap, Suid-Afrika", "Africa/Johannesburg", ["x"]),
            JsonDocument.Parse(expected).RootElement.Clone(), [], DatasetSplit.Dev);

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
        Assert.That(ScenarioShop.Of(Scenario("""{"town":"Paarl","shop":"NAME"}""".Replace("NAME", name))),
            Is.EqualTo(expected));

    /// <summary>A scenario that names no shop has none, and one that names no town has none.</summary>
    [Test]
    public void NoShopOrTown_IsNone()
    {
        Assert.That(ScenarioShop.Of(Scenario("""{"routes_to":null}""")), Is.Null);
        Assert.That(ScenarioShop.TownOf(Scenario("""{"routes_to":null}""")), Is.Null);
        Assert.That(ScenarioShop.TownOf(Scenario("""{"town":" Paarl "}""")), Is.EqualTo("Paarl"));
    }

    /// <summary>A scenario that expects Checkers must name the town too; "any shop" needs none.</summary>
    [Test]
    public void AShopAtATown_NeedsItsTown()
    {
        // The cook's prompt sends the user's town as the area in every call, so a scenario that expects a shop
        // without its town couldn't be graded.
        Assert.That(() => ScenarioShop.TownOf(Scenario("""{"shop":"checkers"}""")),
            Throws.TypeOf<InvalidDataException>().With.Message.Contains("'town'"));
        Assert.That(ScenarioShop.TownOf(Scenario("""{"shop":"any"}""")), Is.Null,
            "no shop is searched, so no town is needed");
    }

    /// <summary>A shop or a town of the wrong kind is refused, naming the key and the scenario.</summary>
    [TestCase("""{"shop":"gremlins"}""", "shop")]
    [TestCase("""{"shop":"none"}""", "shop")]
    [TestCase("""{"shop":1}""", "shop")]
    [TestCase("""{"town":7}""", "town")]
    [TestCase("""{"town":""}""", "town")]
    public void AShopOrTownOfTheWrongKind_IsRefusedByItsKey(string expected, string key) =>
        Assert.That(() =>
            {
                ScenarioShop.Of(Scenario(expected));
                ScenarioShop.TownOf(Scenario(expected));
            },
            Throws.TypeOf<InvalidDataException>().With.Message.Contains($"'{key}'").And.Message.Contains("'cook-x'"));
}
