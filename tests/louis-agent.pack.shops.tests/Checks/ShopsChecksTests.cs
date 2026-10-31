using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Packs.Shops.Checks;

namespace LouisAgent.Packs.Shops.Tests.Checks;

/// <summary>
/// The pack's checks can be named from a file: each is in the pack's list under the name its stored runs carry, and
/// builds the check at the version those runs were graded with, through the reader the file names where it takes one.
/// </summary>
public class ShopsChecksTests
{
    /// <summary>The readers a file here can name: one price reader.</summary>
    private static readonly ReaderCatalog Readers = new([FakePrices.None]);

    /// <summary>
    /// The pack's list holds its shop rules, in the order the cook's eval names them, each a fixed rule.
    /// </summary>
    [Test]
    public void ThePacksList_HoldsItsShopRules()
    {
        var listing = new CheckCatalog(ShopsChecks.All).Listing();

        Assert.That(listing.Select(line => line.Type), Is.EqualTo(new[] { "estimates-at-shop", "untooled-shops" }));
        Assert.That(listing.Select(line => line.Kind), Has.All.EqualTo(CheckKind.FixedRule));
    }

    /// <summary>Each type, from its settings, builds today's check under today's name, version and required.</summary>
    [TestCase("estimates-at-shop", """{"reader": "fake-prices"}""", typeof(EstimatesAtShop), 2)]
    [TestCase("untooled-shops", "{}", typeof(UntooledShops), 1)]
    public void EachType_BuildsTodaysCheck(string type, string settings, Type check, int version)
    {
        var built = Build(type, settings);

        Assert.That(built, Is.TypeOf(check));
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo((type, version, true)));
    }

    /// <summary>
    /// A setting a type doesn't take, a blank reader or a reader the catalogue doesn't have is refused, naming what
    /// was wrong.
    /// </summary>
    [TestCase("untooled-shops", """{"limit": 100}""", "limit")]
    [TestCase("estimates-at-shop", """{"reader": " "}""", "reader")]
    [TestCase("estimates-at-shop", """{"reader": "rand-total"}""", "rand-total")]
    public void ABadSetting_IsRefused(string type, string settings, string named) =>
        Assert.That(Assert.Throws<InvalidDataException>(() => Build(type, settings))!.Message, Does.Contain(named));

    /// <summary>
    /// Builds a check the way an eval file will: from the pack's list, by its type, from its settings, with the
    /// readers to name from.
    /// </summary>
    private static ICheck Build(string type, string settings) =>
        new CheckCatalog(ShopsChecks.All).Get(type)
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), new CheckContext(string.Empty, [], Readers));
}
