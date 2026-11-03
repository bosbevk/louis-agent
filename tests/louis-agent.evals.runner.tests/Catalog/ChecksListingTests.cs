using System.Text.Json;
using LouisAgent.Choices;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Readers;
using LouisAgent.Evals.Runner.Catalog;

namespace LouisAgent.Evals.Runner.Tests.Catalog;

/// <summary>
/// The runner's <c>--checks</c> lines, what the studio composes an eval from (F49.5): one line per check type in the
/// catalogue's order, with its kind's wire name and the JSON schema of its settings, then one line per reader with its
/// kind.
/// </summary>
public class ChecksListingTests
{
    /// <summary>The lines as JSON, read back.</summary>
    private static readonly IReadOnlyList<JsonElement> Lines =
    [
        .. ChecksListing.Lines(RunnerChecks.Catalog, RunnerReaders.Catalog)
            .Select(line => JsonDocument.Parse(line).RootElement.Clone())
    ];

    /// <summary>The lines that are checks, the ones with a type.</summary>
    private static IEnumerable<JsonElement> Checks => Lines.Where(line => line.TryGetProperty("type", out _));

    /// <summary>The lines that are readers.</summary>
    private static IEnumerable<JsonElement> Readers => Lines.Where(line => line.TryGetProperty("reader", out _));

    /// <summary>Every check type of the runner's catalogue is a line, in its order, before any reader.</summary>
    [Test]
    public void EveryCheck_IsALine_InTheCataloguesOrder_BeforeTheReaders()
    {
        Assert.That(Checks.Select(line => line.GetProperty("type").GetString()),
            Is.EqualTo(RunnerChecks.Catalog.Listing().Select(check => check.Type)));
        Assert.That(Lines.TakeWhile(line => line.TryGetProperty("type", out _)).Count(),
            Is.EqualTo(Checks.Count()), "the checks come first");
    }

    /// <summary>A check's kind is its wire name, and its settings are a JSON schema object.</summary>
    [Test]
    public void ACheck_CarriesItsKindsWireName_AndItsSettingsSchema()
    {
        var kinds = SettingChoice.Names<CheckKind>().ToHashSet();

        Assert.That(Checks.Select(line => line.GetProperty("kind").GetString()),
            Is.All.Matches<string>(kinds.Contains));
        Assert.That(Checks.Select(line => line.GetProperty("parameters").ValueKind),
            Is.All.EqualTo(JsonValueKind.Object));
    }

    /// <summary>
    /// A setting the record requires is in its schema's <c>required</c> list, which the studio's form reads to mark
    /// the field: <c>amount-under</c> needs its key and its reader.
    /// </summary>
    [Test]
    public void ARequiredSetting_IsInTheSchemasRequiredList()
    {
        var amountUnder = Checks.Single(line => line.GetProperty("type").GetString() == "amount-under");

        var required = amountUnder.GetProperty("parameters").GetProperty("required").EnumerateArray()
            .Select(name => name.GetString());

        Assert.That(required, Is.SupersetOf(new[] { "key", "reader" }));
    }

    /// <summary>Every reader the runner knows is a line, with its kind's wire name.</summary>
    [Test]
    public void EveryReader_IsALine_WithItsKind()
    {
        Assert.That(Readers.Select(line => line.GetProperty("reader").GetString()),
            Is.EquivalentTo(RunnerReaders.Catalog.Listing().Select(reader => reader.Name)));
        Assert.That(Readers.Select(line => line.GetProperty("kind").GetString()),
            Is.All.Matches<string>(SettingChoice.Names<ReaderKind>().Contains));
    }
}
