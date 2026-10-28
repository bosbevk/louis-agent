using System.Text.Json;
using LouisAgent.Api.Evals.Reports;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Turns;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Turns;

/// <summary>
/// A turn's Mongo document is a record the driver maps (ADR 0048), its stream events inside. It writes exactly what
/// the hand-written mapper before it wrote, so the change moved the code and not the data, and it reads its own
/// document back as the turn and the events it was. The fixtures are what that mapper wrote for a turn with every
/// field set and for a bare one. Stored eval data is disposable until Phase 9 (ADR 0049), so no document from before
/// is read.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public class TurnDocumentTests
{
    /// <summary>The run the turns belong to.</summary>
    private const string Run = "run123456789";

    /// <summary>When the question was asked; whole milliseconds, as a BSON date keeps it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 8, 0, 0, 125, TimeSpan.Zero);

    /// <summary>A second turn that failed after its tools ran, with every field set.</summary>
    private static readonly Turn Full = new(new TurnKey(SampleMode.Alpha, 2, "cook-af", 2), "sess_2", "En vir ses?",
        "Hier is bobotie vir ses.", SampleRole.Alpha, "sub-1", 3, 2, ["www.checkers.co.za", "www.pnp.co.za"], 7.75,
        "the model timed out", new EvalTools.Summary(4, 1, [new EvalTools.Failure("ReadRenderedPage", "refused", 1)]),
        TurnFailure.Timeout, T0, T0.AddSeconds(8));

    /// <summary>A first turn with nothing but its question: no answer, tools, failure or times.</summary>
    private static readonly Turn Bare = new(new TurnKey(SampleMode.Beta, 1, "cook-en", 1), "sess_1", "Bobotie?", null,
        null, null, 0, 0, [], 0, null);

    /// <summary>
    /// The full turn's events: text with its time, and a last one whose data holds every kind of JSON value and which
    /// has no time.
    /// </summary>
    private static readonly StreamEvent[] FullEvents =
    [
        new("text", Json("""{"delta":{"text":"Hier"}}"""), T0.AddSeconds(1)),
        new("done", Json("""{"n":[1,2.5,true,null,"x"]}"""), null),
    ];

    /// <summary>Parses a bit of JSON into a value we can keep.</summary>
    private static JsonElement Json(string json) => JsonDocument.Parse(json).RootElement.Clone();

    /// <summary>A fixture beside this test in the repo, so a regenerated one lands in git.</summary>
    private static string FixturePath(string name) =>
        Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.api.tests", "Evals", "Store", "Turns", "Fixtures",
            name);

    /// <summary>A fixture as the document it holds, every BSON type kept.</summary>
    private static BsonDocument Fixture(string name) => BsonDocument.Parse(File.ReadAllText(FixturePath(name)));

    /// <summary>A value's fields as text, since a record holds its lists by reference.</summary>
    private static string Fields(object value) => JsonSerializer.Serialize(value);

    /// <summary>Registers the store's mappings, as the store does before its first collection.</summary>
    [SetUp]
    public void Register() => MongoEvalDatabase.RegisterMappings();

    /// <summary>The turn and its events a fixture holds.</summary>
    private static (Turn Turn, StreamEvent[] Events) Expected(string fixture) =>
        fixture == "turn-full.json" ? (Full, FullEvents) : (Bare, []);

    /// <summary>A turn reads back from its own document as the turn it was, its events left out.</summary>
    [TestCase("turn-full.json")]
    [TestCase("turn-bare.json")]
    public void ATurn_ReadsBackFromItsOwnDocument(string fixture)
    {
        var (turn, events) = Expected(fixture);
        var written = TurnDocument.Of(Run, turn, events).ToBsonDocument();

        var read = BsonSerializer.Deserialize<TurnDocument>(written).ToTurn();

        Assert.That(Fields(read), Is.EqualTo(Fields(turn)));
    }

    /// <summary>
    /// A turn's events read back from its document in the order they came, each with its data and time.
    /// </summary>
    [Test]
    public void ATurnsEvents_ReadBackFromItsOwnDocument()
    {
        var written = TurnDocument.Of(Run, Full, FullEvents).ToBsonDocument();

        var read = BsonSerializer.Deserialize<TurnDocument>(written).ToEvents();

        Assert.That(Fields(read), Is.EqualTo(Fields(FullEvents)));
    }

    /// <summary>
    /// A conversation's turns are read without their events (the store leaves them out), and a turn still reads whole
    /// with no events of its own.
    /// </summary>
    [Test]
    public void ATurnReadWithoutItsEvents_IsTheTurn_WithNoEvents()
    {
        var written = TurnDocument.Of(Run, Full, FullEvents).ToBsonDocument();
        written.Remove("events");

        var read = BsonSerializer.Deserialize<TurnDocument>(written);

        Assert.That(Fields(read.ToTurn()), Is.EqualTo(Fields(Full)));
        Assert.That(read.ToEvents(), Is.Empty);
    }

    /// <summary>A turn's events are read on their own (the store asks for nothing else), and they read whole.</summary>
    [Test]
    public void ATurnsEventsReadAlone_ReadWhole()
    {
        var written = TurnDocument.Of(Run, Full, FullEvents).ToBsonDocument();
        var eventsOnly = new BsonDocument
        {
            ["_id"] = written["_id"],
            ["events"] = written["events"]
        };

        var read = BsonSerializer.Deserialize<TurnDocument>(eventsOnly).ToEvents();

        Assert.That(Fields(read), Is.EqualTo(Fields(FullEvents)));
    }

    /// <summary>
    /// A turn is written as the old mapper wrote it: the same fields, in the same order, of the same types.
    /// </summary>
    [TestCase("turn-full.json")]
    [TestCase("turn-bare.json")]
    public void ATurn_IsWrittenAsTheOldMapperWroteIt(string fixture)
    {
        var (turn, events) = Expected(fixture);

        var written = TurnDocument.Of(Run, turn, events).ToBsonDocument();

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
