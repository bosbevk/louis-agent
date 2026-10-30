using System.Text.Json.Nodes;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Tests.Datasets;

namespace LouisAgent.Evals.Tests.Files;

/// <summary>
/// The eval file's reader: an <c>eval.json</c> reads strictly into its records, and anything it can't trust is
/// refused, naming the file and, where there is one, the field or the name.
/// </summary>
public class EvalFileReaderTests
{
    /// <summary>The plan's cook file: two modes, nine checks, one keeping an old name, some with settings.</summary>
    private const string CookFile =
        """
        {
          "schema_version": 1,
          "name": "puls-cook",
          "identity": "puls",
          "version": 11,
          "proves": "Puls gives a meal for the people asked, with estimates and the shop's branches, in Afrikaans or English.",
          "manual": "manual.md",
          "modes": [
            { "name": "puls-tools", "role": "puls/voice-af", "toolsets": ["web", "shops"] },
            { "name": "puls-cook", "role": "puls/voice-af", "toolsets": ["specialists", "web"] }
          ],
          "checks": [
            { "type": "no-error", "name": "cook-no-error" },
            { "type": "routed-to", "parameters": { "expected_key": "routes_to", "delegating_toolsets": ["specialists", "minions"] } },
            { "type": "within-budget" },
            { "type": "estimates-at-shop" },
            { "type": "untooled-shops" },
            { "type": "recipe-image" },
            { "type": "serves-as-asked" },
            { "type": "meal-for-people", "parameters": { "rubric": "rubrics/meal-for-people.md", "version": 2 } },
            { "type": "human", "parameters": { "reviews": "meal-for-people" } }
          ]
        }
        """;

    /// <summary>The cook's file reads into the records, field by field.</summary>
    [Test]
    public void TheCooksFile_ReadsIntoTheRecords()
    {
        using var folder = new DatasetFolder().Write("eval.json", CookFile);

        var eval = EvalFileReader.Read(folder.Root);

        Assert.That((eval.SchemaVersion, eval.Name, eval.Identity, eval.Version, eval.Manual),
            Is.EqualTo((1, "puls-cook", "puls", 11, "manual.md")));
        Assert.That(eval.Modes.Select(mode => mode.Name), Is.EqualTo(new[] { "puls-tools", "puls-cook" }));
        Assert.That(eval.Modes[1].Toolsets, Is.EqualTo(new[] { "specialists", "web" }));
        Assert.That(eval.Checks, Has.Count.EqualTo(9));
        Assert.That((eval.Checks[0].Type, eval.Checks[0].Name), Is.EqualTo(("no-error", "cook-no-error")));
        Assert.That(eval.Checks[1].Parameters?.GetProperty("expected_key").GetString(), Is.EqualTo("routes_to"));
        Assert.That((eval.Checks[2].Type, eval.Checks[2].Name), Is.EqualTo(("within-budget", (string?)null)));
        Assert.That(eval.Checks[2].Parameters, Is.Null);
    }

    /// <summary>A folder with no <c>eval.json</c> is refused, naming the folder.</summary>
    [Test]
    public void NoFile_IsRefused()
    {
        using var folder = new DatasetFolder();

        Assert.That(Refusal(folder), Does.Contain(folder.Root));
    }

    /// <summary>A file that isn't JSON at all is refused, naming the file.</summary>
    [Test]
    public void NotJson_IsRefused()
    {
        using var folder = new DatasetFolder().Write("eval.json", "{ not json");

        Assert.That(Refusal(folder), Does.Contain("eval.json"));
    }

    /// <summary>A missing field, a null one and an unknown one are refused, naming the file and the field.</summary>
    [TestCase("missing", "proves")]
    [TestCase("null", "name")]
    [TestCase("unknown", "threshold")]
    public void AFieldThatDoesNotFit_IsRefused_NamingIt(string how, string field)
    {
        using var folder = Cook(file =>
        {
            switch (how)
            {
                case "missing":
                    file.Remove(field);
                    break;
                case "null":
                    file[field] = null;
                    break;
                case "unknown":
                    file["checks"]![2]![field] = 0.8;
                    break;
            }
        });

        Assert.That(Refusal(folder), Does.Contain("eval.json").And.Contain(field));
    }

    /// <summary>A schema version other than 1 is refused, naming the version the file gave.</summary>
    [Test]
    public void AnotherSchemaVersion_IsRefused()
    {
        using var folder = Cook(file => file["schema_version"] = 2);

        Assert.That(Refusal(folder), Does.Contain("eval.json").And.Contain("2"));
    }

    /// <summary>
    /// An eval with no mode, or no check, has nothing to run or nothing to grade with, so it is refused.
    /// </summary>
    [TestCase("modes")]
    [TestCase("checks")]
    public void AnEmptyList_IsRefused(string list)
    {
        using var folder = Cook(file => file[list] = new JsonArray());

        Assert.That(Refusal(folder), Does.Contain("eval.json").And.Contain(list));
    }

    /// <summary>Two modes under one name are refused, naming it.</summary>
    [Test]
    public void AModeTwice_IsRefused()
    {
        using var folder = Cook(file => file["modes"]![0]!["name"] = "puls-cook");

        Assert.That(Refusal(folder), Does.Contain("eval.json").And.Contain("puls-cook"));
    }

    /// <summary>Two checks under one name are refused, naming it: the name it was given, else its type.</summary>
    [TestCase("""{ "type": "within-budget" }""", "within-budget")]
    [TestCase("""{ "type": "contains", "name": "cook-no-error" }""", "cook-no-error")]
    public void ACheckTwice_IsRefused_ByItsName(string entry, string name)
    {
        using var folder = Cook(file => file["checks"]!.AsArray().Add(JsonNode.Parse(entry)));

        Assert.That(Refusal(folder), Does.Contain("eval.json").And.Contain(name));
    }

    /// <summary>A folder holding the cook's file with one change made to it.</summary>
    private static DatasetFolder Cook(Action<JsonObject> change)
    {
        var file = JsonNode.Parse(CookFile)!.AsObject();
        change(file);
        return new DatasetFolder().Write("eval.json", file.ToJsonString());
    }

    /// <summary>The reader's refusal for the folder's file.</summary>
    private static string Refusal(DatasetFolder folder) =>
        Assert.Throws<InvalidDataException>(() => EvalFileReader.Read(folder.Root))!.Message;
}
