using System.Text.Json;
using LouisAgent.Api.Evals.Calibrations;
using LouisAgent.Api.Evals.Calibrations.Store;
using LouisAgent.Api.Evals.Store.Mongo;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace LouisAgent.Api.Tests.Evals.Calibrations.Store;

/// <summary>
/// A judge check's Mongo document is a record the driver maps (ADR 0048). It writes exactly what the hand-written
/// mapping before it wrote, so the change moved the code and not the data, and it reads its own document back as the
/// report it was. The fixtures were written by hand from that mapping (it had no method of its own to call): a failed
/// check with its judge model and two disagreements, and an empty one with no model. Stored eval data is disposable
/// until Phase 9 (ADR 0049), so no document from before is read.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public class CalibrationDocumentTests
{
    /// <summary>When the check ran; whole milliseconds, as a BSON date keeps it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 8, 0, 0, 125, TimeSpan.Zero);

    /// <summary>A check the judge failed: 16 of 18 agreed under a bar of 13 per class, with its two misses.</summary>
    private static readonly CalibrationRecord Full = new("run123456789", "meal-for-people", "qwen3:8b", T0, 16, 18, 13,
        false, new CalibrationClassCounts(13, 1, 0), new CalibrationClassCounts(1, 3, 0),
        [
            new CalibrationDisagreement("cook-af", SampleMode.Alpha, 1, "pass", "fail", "said it feeds two"),
            new CalibrationDisagreement("cook-en", SampleMode.Alpha, 2, "fail", "pass", "a full meal for six"),
        ]);

    /// <summary>A check with nothing to compare yet, and no judge model known.</summary>
    private static readonly CalibrationRecord Bare = new("run123456789", "meal-for-people", null, T0, 0, 0, 0, false,
        new CalibrationClassCounts(0, 0, 0), new CalibrationClassCounts(0, 0, 0), []);

    /// <summary>A fixture beside this test in the repo.</summary>
    private static BsonDocument Fixture(string name) =>
        BsonDocument.Parse(File.ReadAllText(Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.api.tests",
            "Evals", "Calibrations", "Store", "Fixtures", name)));

    /// <summary>The report's fields as text, since a record holds its lists by reference.</summary>
    private static string Fields(CalibrationRecord report) => JsonSerializer.Serialize(report);

    /// <summary>Registers the store's mappings, as the store does before its first collection.</summary>
    [SetUp]
    public void Register() => MongoEvalDatabase.RegisterMappings();

    /// <summary>The report a fixture holds.</summary>
    private static CalibrationRecord Expected(string fixture) => fixture == "calibration-full.json" ? Full : Bare;

    /// <summary>A report reads back from its own document as the report it was.</summary>
    [TestCase("calibration-full.json")]
    [TestCase("calibration-bare.json")]
    public void AReport_ReadsBackFromItsOwnDocument(string fixture)
    {
        var report = Expected(fixture);
        var written = CalibrationDocument.Of(report).ToBsonDocument();

        var read = BsonSerializer.Deserialize<CalibrationDocument>(written).ToRecord();

        Assert.That(Fields(read), Is.EqualTo(Fields(report)));
    }

    /// <summary>
    /// A report read back from Mongo carries the id Mongo gave it on insert; the record has none, and reads past it.
    /// </summary>
    [Test]
    public void AReportWithMongosOwnId_ReadsBack()
    {
        var stored = CalibrationDocument.Of(Full).ToBsonDocument();
        stored.InsertAt(0, new BsonElement("_id", ObjectId.GenerateNewId()));

        var read = BsonSerializer.Deserialize<CalibrationDocument>(stored).ToRecord();

        Assert.That(Fields(read), Is.EqualTo(Fields(Full)));
    }

    /// <summary>
    /// A report is written as the old mapping wrote it: the same fields, in the same order, of the same types.
    /// </summary>
    [TestCase("calibration-full.json")]
    [TestCase("calibration-bare.json")]
    public void AReport_IsWrittenAsTheOldMappingWroteIt(string fixture)
    {
        var written = CalibrationDocument.Of(Expected(fixture)).ToBsonDocument();

        Assert.That(written.ToJson(Canonical), Is.EqualTo(Fixture(fixture).ToJson(Canonical)));
    }

    /// <summary>How a document is compared: canonical extended JSON keeps every BSON type.</summary>
    private static readonly JsonWriterSettings Canonical =
        new()
        {
            OutputMode = JsonOutputMode.CanonicalExtendedJson,
            Indent = true
        };
}
