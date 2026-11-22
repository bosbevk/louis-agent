using System.Text.Json.Nodes;
using LouisAgent.Choices;
using LouisAgent.Evals.Files;
using LouisAgent.Evals.Files.Json;

namespace LouisAgent.Evals.Tests.Files.Json;

/// <summary>
/// The eval file's schema is made from the records the reader fills (F126.2 step 7, Louis's pick, "Generated from the
/// records"), kept as a golden file an editor reads, and holds what a person leans on while writing a file.
/// </summary>
public class EvalSchemaTests
{
    /// <summary>Where the schema an eval file names lives.</summary>
    private const string SchemaFile = "src/evals/LouisAgent.Evals/eval.schema.json";

    /// <summary>The schema, as the records make it.</summary>
    private static readonly JsonObject Schema = JsonNode.Parse(EvalSchema.Write())!.AsObject();

    /// <summary>The file in the repository is what the records say.</summary>
    [Test]
    public void TheSchemaFile_IsWhatTheRecordsSay() => Golden.Match(SchemaFile, EvalSchema.Write());

    /// <summary>No level takes a field it doesn't know, so a misspelt one is underlined.</summary>
    [Test]
    public void NoLevel_TakesAFieldItDoesntKnow() =>
        Assert.That(Objects().Where(level => level["additionalProperties"]?.GetValue<bool>() != false)
            .Select(level => level.ToJsonString()), Is.Empty);

    /// <summary>The fields an eval can't do without are required.</summary>
    [TestCase("", "agent", "conversations")]
    [TestCase("conversations", "id", "turns")]
    [TestCase("turns", "ask")]
    public void TheFieldsAnEvalNeeds_AreRequired(string level, params string[] required) =>
        Assert.That(Required(Level(level)), Is.SupersetOf(required));

    /// <summary>Every field has a description, which the editor shows on hover.</summary>
    [Test]
    public void EveryField_HasADescription() =>
        Assert.That(Objects().SelectMany(level => Properties(level))
            .Where(field => field.Value?["description"] is null).Select(field => field.Key), Is.Empty);

    /// <summary>A file may name its own schema.</summary>
    [Test]
    public void ASchemaPath_IsAllowed() => Assert.That(Schema["properties"]?["$schema"], Is.Not.Null);

    /// <summary>A fact is a string whose pattern holds each of the seven first words, read from the enum.</summary>
    [Test]
    public void AFact_IsAStringThatStartsWithOneOfTheSeven()
    {
        var fact = Level("turns")["properties"]!["facts"]!["items"]!;
        string pattern = fact["pattern"]!.GetValue<string>();

        Assert.Multiple(() =>
        {
            Assert.That(fact["type"]!.GetValue<string>(), Is.EqualTo("string"));
            foreach (string word in SettingChoice.Names<FactKind>())
                Assert.That(pattern, Does.Contain(word));
        });
    }

    /// <summary>The object a level's items are, by the field that holds them, or the root for none.</summary>
    private static JsonObject Level(string field) => field switch
    {
        "" => Schema,
        "conversations" => Schema["properties"]!["conversations"]!["items"]!.AsObject(),
        "turns" => Level("conversations")["properties"]!["turns"]!["items"]!.AsObject(),
        _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
    };

    /// <summary>Every object the schema describes: the root, a conversation and a turn.</summary>
    private static IEnumerable<JsonObject> Objects() => [Level(""), Level("conversations"), Level("turns")];

    /// <summary>A level's fields.</summary>
    private static IEnumerable<KeyValuePair<string, JsonNode?>> Properties(JsonObject level) =>
        level["properties"]!.AsObject();

    /// <summary>A level's required fields.</summary>
    private static IEnumerable<string> Required(JsonObject level) =>
        level["required"]?.AsArray().Select(name => name!.GetValue<string>()) ?? [];
}
