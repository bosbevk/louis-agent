using System.Diagnostics;
using LouisAgent.Evals.Catalog;
using LouisAgent.Evals.Grading;
using LouisAgent.Evals.Protocol.Kinds;

namespace LouisAgent.Evals.Checks.Tests.Catalog;

/// <summary>
/// The catalogue finds a check type by its name, refuses a list it can't trust, and lists every entry with the
/// schema of its settings.
/// </summary>
public class CheckCatalogTests
{
    private static readonly TestEntry TimeLimit = new("time-limit", CheckKind.FixedRule);
    private static readonly TestEntry Judge = new("judge", CheckKind.Model);

    /// <summary>Each entry is found by its type; a type that isn't there is simply not found.</summary>
    [Test]
    public void FindsEachEntry_ByItsType()
    {
        var catalog = new CheckCatalog([TimeLimit, Judge]);

        Assert.That(catalog.Find("time-limit"), Is.SameAs(TimeLimit));
        Assert.That(catalog.Get("judge"), Is.SameAs(Judge));
        Assert.That(catalog.Find("nope"), Is.Null);
    }

    /// <summary>Asking for a type the catalogue doesn't have is refused, naming the types it does have.</summary>
    [Test]
    public void AnUnknownType_IsRefused_NamingTheKnownOnes()
    {
        var catalog = new CheckCatalog([TimeLimit, Judge]);

        var refusal = Assert.Throws<CheckCatalogException>(() => catalog.Get("nope"));

        Assert.That(refusal!.Message, Does.Contain("nope").And.Contain("time-limit").And.Contain("judge"));
    }

    /// <summary>Two entries with one type are refused: a file couldn't say which it meant.</summary>
    [Test]
    public void ATypeListedTwice_IsRefused()
    {
        var refusal = Assert.Throws<CheckCatalogException>(() =>
            _ = new CheckCatalog([TimeLimit, new TestEntry("time-limit", CheckKind.Model)]));

        Assert.That(refusal!.Message, Does.Contain("time-limit"));
    }

    /// <summary>An empty type, or one with a character an MCP tool name can't carry, is refused.</summary>
    [TestCase("")]
    [TestCase("time limit")]
    [TestCase("time.limit")]
    public void AnUnsafeType_IsRefused(string type)
    {
        Assert.Throws<CheckCatalogException>(() => _ = new CheckCatalog([new TestEntry(type, CheckKind.FixedRule)]));
    }

    /// <summary>The listing gives each entry, in order, with a schema that names every field of its settings.</summary>
    [Test]
    public void TheListing_HasEachEntry_WithItsSettingsSchema()
    {
        var listing = new CheckCatalog([TimeLimit, Judge]).Listing();

        Assert.That(listing.Select(line => line.Type), Is.EqualTo(new[] { "time-limit", "judge" }));
        Assert.That(listing[1].Kind, Is.EqualTo(CheckKind.Model));
        var fields = listing[0].Parameters["properties"]!.AsObject().Select(field => field.Key);
        Assert.That(fields, Is.EquivalentTo(new[] { "seconds", "required" }));
    }

    /// <summary>
    /// The test's settings: a limit, and whether the check must pass, with a default. Only its schema is read, by
    /// reflection, so nothing creates one.
    /// </summary>
    private sealed record LimitParameters(int Seconds, bool Required = true);

    /// <summary>An entry for the test; the catalogue never builds a check, so building one is never reached.</summary>
    private sealed class TestEntry(string type, CheckKind kind) : CheckType<LimitParameters>(type, kind)
    {
        /// <summary>Never called: these tests are about finding and listing entries.</summary>
        protected override ICheck Create(LimitParameters parameters, CheckContext context) =>
            throw new UnreachableException();
    }
}
