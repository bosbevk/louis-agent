using System.Text.Json;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Requests;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Requests;

/// <summary>
/// A model request's Mongo document is a record the driver maps (ADR 0048). It writes exactly what the hand-written
/// mapper before it wrote, so the change moved the code and not the data, and it reads its own document back as the
/// request it was. The fixtures are what that mapper wrote for a priced request tied to its turn and for one with
/// nothing known. Stored eval data is disposable until Phase 9 (ADR 0049), so no document from before is read.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0007-measure-every-request.md">ADR 0007: measure every request</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public class RequestDocumentTests
{
    /// <summary>When the request was made; whole milliseconds, as a BSON date keeps it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 8, 0, 0, 125, TimeSpan.Zero);

    /// <summary>A sub-session's priced request, tied to its turn, every count known.</summary>
    private static readonly Request Full = new("run123456789", new TurnKey(SampleMode.Alpha, 2, "cook-af", 1), T0,
        "turn", "sess_2", "sess_1", "claude-haiku-5-5", 1200, 340, 800, 64, 0.0042m, 2350);

    /// <summary>A request the ledger knew little about: no turn, session, model, counts or price.</summary>
    private static readonly Request Bare = new("run123456789", null, T0, "summary", null, null, null, null, null,
        null, null, null, 0);

    /// <summary>A fixture beside this test in the repo, so a regenerated one lands in git.</summary>
    private static string FixturePath(string name) =>
        Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.api.tests", "Evals", "Store", "Requests",
            "Fixtures", name);

    /// <summary>A fixture as the document it holds, every BSON type kept.</summary>
    private static BsonDocument Fixture(string name) => BsonDocument.Parse(File.ReadAllText(FixturePath(name)));

    /// <summary>The request's fields as text, so two requests compare by what they hold.</summary>
    private static string Fields(Request request) => JsonSerializer.Serialize(request);

    /// <summary>Registers the store's mappings, as the store does before its first collection.</summary>
    [SetUp]
    public void Register() => MongoEvalDatabase.RegisterMappings();

    /// <summary>The request a fixture holds.</summary>
    private static Request Expected(string fixture) => fixture == "request-full.json" ? Full : Bare;

    /// <summary>A request reads back from its own document as the request it was.</summary>
    [TestCase("request-full.json")]
    [TestCase("request-bare.json")]
    public void ARequest_ReadsBackFromItsOwnDocument(string fixture)
    {
        var request = Expected(fixture);
        var written = RequestDocument.Of(request).ToBsonDocument();

        var read = BsonSerializer.Deserialize<RequestDocument>(written).ToRequest();

        Assert.That(Fields(read), Is.EqualTo(Fields(request)));
    }

    /// <summary>
    /// A request read back from Mongo carries the id Mongo gave it on insert; the record has none, and reads past it.
    /// </summary>
    [Test]
    public void ARequestWithMongosOwnId_ReadsBack()
    {
        var stored = RequestDocument.Of(Full).ToBsonDocument();
        stored.InsertAt(0, new BsonElement("_id", ObjectId.GenerateNewId()));

        var read = BsonSerializer.Deserialize<RequestDocument>(stored).ToRequest();

        Assert.That(Fields(read), Is.EqualTo(Fields(Full)));
    }

    /// <summary>
    /// A request is written as the old mapper wrote it: the same fields, in the same order, of the same types.
    /// </summary>
    [TestCase("request-full.json")]
    [TestCase("request-bare.json")]
    public void ARequest_IsWrittenAsTheOldMapperWroteIt(string fixture)
    {
        var written = RequestDocument.Of(Expected(fixture)).ToBsonDocument();

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
