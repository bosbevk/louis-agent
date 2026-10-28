using System.Text.Json;
using LouisAgent.Api.Evals.Reviews;
using LouisAgent.Api.Evals.Reviews.Store;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace LouisAgent.Api.Tests.Evals.Reviews.Store;

/// <summary>
/// A person's grade's Mongo document is a record the driver maps (ADR 0048). It writes exactly what the hand-written
/// mapping before it wrote, so the change moved the code and not the data, and it reads its own document back as the
/// grade it was, whatever the verdict. The fixture was written by hand from that mapping (it had no method of its own
/// to call). Stored eval data is disposable until Phase 9 (ADR 0049), so no document from before is read.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public class HumanGradeDocumentTests
{
    /// <summary>When the person graded; whole milliseconds, as a BSON date keeps it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 8, 0, 0, 125, TimeSpan.Zero);

    /// <summary>A person failing the meal judge's conversation, with a note.</summary>
    private static readonly HumanGrade Failed = new("run123456789", new ConversationKey(SampleMode.Alpha, 2, "cook-af"),
        "meal-for-people.human", HumanVerdict.Fail, "feeds two, not six", "reviewer-a", T0);

    /// <summary>The fixture beside this test in the repo.</summary>
    private static BsonDocument Fixture() =>
        BsonDocument.Parse(File.ReadAllText(Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.api.tests",
            "Evals", "Reviews", "Store", "Fixtures", "human-grade.json")));

    /// <summary>The grade's fields as text, so two grades compare by what they hold.</summary>
    private static string Fields(HumanGrade grade) => JsonSerializer.Serialize(grade);

    /// <summary>Registers the store's mappings, as the store does before its first collection.</summary>
    [SetUp]
    public void Register() => MongoEvalDatabase.RegisterMappings();

    /// <summary>A grade reads back from its own document as the grade it was, for every verdict.</summary>
    [TestCase(HumanVerdict.Pass)]
    [TestCase(HumanVerdict.Fail)]
    [TestCase(HumanVerdict.Unsure)]
    public void AGrade_ReadsBackFromItsOwnDocument(HumanVerdict verdict)
    {
        var grade = Failed with { Verdict = verdict };
        var written = HumanGradeDocument.Of(grade).ToBsonDocument();

        var read = BsonSerializer.Deserialize<HumanGradeDocument>(written).ToGrade();

        Assert.That(Fields(read), Is.EqualTo(Fields(grade)));
    }

    /// <summary>
    /// A grade read back from Mongo carries the id Mongo gave it on insert; the record has none, and reads past it.
    /// </summary>
    [Test]
    public void AGradeWithMongosOwnId_ReadsBack()
    {
        var stored = HumanGradeDocument.Of(Failed).ToBsonDocument();
        stored.InsertAt(0, new BsonElement("_id", ObjectId.GenerateNewId()));

        var read = BsonSerializer.Deserialize<HumanGradeDocument>(stored).ToGrade();

        Assert.That(Fields(read), Is.EqualTo(Fields(Failed)));
    }

    /// <summary>
    /// A grade is written as the old mapping wrote it: the same fields, in the same order, of the same types, the
    /// verdict by its wire name.
    /// </summary>
    [Test]
    public void AGrade_IsWrittenAsTheOldMappingWroteIt()
    {
        var written = HumanGradeDocument.Of(Failed).ToBsonDocument();

        Assert.That(written.ToJson(Canonical), Is.EqualTo(Fixture().ToJson(Canonical)));
    }

    /// <summary>How a document is compared: canonical extended JSON keeps every BSON type.</summary>
    private static readonly JsonWriterSettings Canonical =
        new()
        {
            OutputMode = JsonOutputMode.CanonicalExtendedJson,
            Indent = true
        };
}
