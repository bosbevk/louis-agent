using System.Text.Json;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Packs.Shops.Checks;

namespace LouisAgent.Packs.Shops.Tests.Checks;

/// <summary>
/// The pack's checks can be named from a file: each is in the pack's list under the name its stored runs carry, and
/// builds the check at the version those runs were graded with.
/// </summary>
public class ShopsChecksTests
{
    /// <summary>The pack's list holds its shop rules, each a fixed rule.</summary>
    [Test]
    public void ThePacksList_HoldsItsShopRules()
    {
        var listing = new CheckCatalog(ShopsChecks.All).Listing();

        Assert.That(listing.Select(line => line.Type), Is.EqualTo(new[] { "untooled-shops" }));
        Assert.That(listing.Select(line => line.Kind), Has.All.EqualTo(CheckKind.FixedRule));
    }

    /// <summary>The type, with no settings, builds today's check under today's name, version and required.</summary>
    [Test]
    public void TheType_BuildsTodaysCheck()
    {
        var built = Build("untooled-shops", "{}");

        Assert.That(built, Is.TypeOf<UntooledShops>());
        Assert.That((built.Name, built.Version, built.Required), Is.EqualTo(("untooled-shops", 1, true)));
    }

    /// <summary>A setting it doesn't take is refused, naming the check type and the field.</summary>
    [Test]
    public void ASettingItDoesntTake_IsRefused()
    {
        var refusal = Assert.Throws<InvalidDataException>(() => Build("untooled-shops", """{"limit": 100}"""));

        Assert.That(refusal!.Message, Does.Contain("untooled-shops").And.Contain("limit"));
    }

    /// <summary>
    /// Builds a check the way an eval file will: from the pack's list, by its type, from its settings.
    /// </summary>
    private static ICheck Build(string type, string settings) =>
        new CheckCatalog(ShopsChecks.All).Get(type)
            .Create(JsonDocument.Parse(settings).RootElement.Clone(), CheckContext.None);
}
