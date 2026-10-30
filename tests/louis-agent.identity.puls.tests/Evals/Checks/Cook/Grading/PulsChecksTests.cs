using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading;
using LouisAgent.Identities.Puls.Evals.Checks.Cook.Grading.WhereToBuy;

namespace LouisAgent.Identities.Puls.Tests.Evals.Checks.Cook.Grading;

/// <summary>
/// The cook's own checks can be named from a file: each is in Puls's list under the name its stored runs carry, and
/// builds the check at the version those runs were graded with.
/// </summary>
public class PulsChecksTests
{
    /// <summary>
    /// Puls's list holds only the shop checks, until they move to the shops pack; the rest of the cook's rules are the
    /// general checks over Puls's readers, and its judge is in the judges' own list, which needs the harness.
    /// </summary>
    [Test]
    public void PulssList_HoldsTheShopChecks()
    {
        var listing = new CheckCatalog(PulsChecks.All).Listing();

        Assert.That(listing.Select(line => line.Type), Is.EqualTo(new[] { "estimates-at-shop", "untooled-shops" }));
        Assert.That(listing.Select(line => line.Kind), Has.All.EqualTo(CheckKind.FixedRule));
    }

    /// <summary>Each type, with no settings, builds today's check under today's name, version and required.</summary>
    [TestCase("estimates-at-shop", typeof(EstimatesAtShop), 2, true)]
    [TestCase("untooled-shops", typeof(UntooledShops), 1, true)]
    public void EachType_BuildsTodaysCheck(string type, Type check, int version, bool required)
    {
        var built = Build(type, "{}");

        Assert.That(built, Is.TypeOf(check));
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo((type, version, required)));
    }

    /// <summary>A setting none of them takes is refused, naming the check type and the field.</summary>
    [Test]
    public void ASettingTheyDontTake_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build("untooled-shops", """{"limit": 100}"""));

        Assert.That(refusal!.Message, Does.Contain("untooled-shops").And.Contain("limit"));
    }

    /// <summary>Builds a check the way an eval file will: from Puls's list, by its type, from its settings.</summary>
    private static ICheck Build(string type, string settings) =>
        new CheckCatalog(PulsChecks.All).Get(type)
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
