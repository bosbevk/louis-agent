using System.Text.Json;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Notes;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Notes;

/// <summary>
/// A note's Mongo document is a record the driver maps (ADR 0048). Its id is the one the driver used to give it on
/// insert, which rises with each note, so notes still come back in the order written. Apart from that id it writes
/// exactly what the hand-written mapper before it wrote, and it reads its own document back as the note it was. The
/// fixtures are what that mapper wrote for a note on a conversation and for one on the whole run. Stored eval data is
/// disposable until Phase 9 (ADR 0049), so no document from before is read.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public class NoteDocumentTests
{
    /// <summary>The run the notes are on.</summary>
    private const string Run = "run123456789";

    /// <summary>When the note was written; whole milliseconds, as a BSON date keeps it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 8, 0, 0, 125, TimeSpan.Zero);

    /// <summary>A note on one conversation.</summary>
    private static readonly RunNote OnAConversation =
        new("this one hallucinated a price", T0, new ConversationKey(SampleMode.Alpha, 2, "cook-af"));

    /// <summary>A note on the whole run, with no conversation.</summary>
    private static readonly RunNote OnTheRun = new("judge changed", T0);

    /// <summary>A fixture beside this test in the repo, so a regenerated one lands in git.</summary>
    private static string FixturePath(string name) =>
        Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.api.tests", "Evals", "Store", "Notes", "Fixtures",
            name);

    /// <summary>A fixture as the document it holds, every BSON type kept.</summary>
    private static BsonDocument Fixture(string name) => BsonDocument.Parse(File.ReadAllText(FixturePath(name)));

    /// <summary>The note's fields as text, so two notes compare by what they hold.</summary>
    private static string Fields(RunNote note) => JsonSerializer.Serialize(note);

    /// <summary>Registers the store's mappings, as the store does before its first collection.</summary>
    [SetUp]
    public void Register() => MongoEvalDatabase.RegisterMappings();

    /// <summary>The note a fixture holds.</summary>
    private static RunNote Expected(string fixture) => fixture == "note-conversation.json" ? OnAConversation : OnTheRun;

    /// <summary>A note reads back from its own document as the note it was.</summary>
    [TestCase("note-conversation.json")]
    [TestCase("note-run.json")]
    public void ANote_ReadsBackFromItsOwnDocument(string fixture)
    {
        var note = Expected(fixture);
        var written = NoteDocument.Of(Run, note).ToBsonDocument();

        var read = BsonSerializer.Deserialize<NoteDocument>(written).ToNote();

        Assert.That(Fields(read), Is.EqualTo(Fields(note)));
    }

    /// <summary>Each note gets a new id, later than the one before, so sorting on it gives the order written.</summary>
    [Test]
    public void EachNote_GetsALaterIdThanTheOneBefore()
    {
        var first = NoteDocument.Of(Run, OnTheRun);
        var second = NoteDocument.Of(Run, OnTheRun);

        Assert.That(second.Id, Is.GreaterThan(first.Id));
    }

    /// <summary>
    /// A note is written as the old mapper wrote it, with the id the driver used to add on insert in first place: the
    /// same fields after it, in the same order, of the same types.
    /// </summary>
    [TestCase("note-conversation.json")]
    [TestCase("note-run.json")]
    public void ANote_IsWrittenAsTheOldMapperWroteIt_WithItsIdFirst(string fixture)
    {
        var written = NoteDocument.Of(Run, Expected(fixture)).ToBsonDocument();

        Assert.That(written.GetElement(0), Is.EqualTo(new BsonElement("_id", written["_id"].AsObjectId)));
        written.Remove("_id");
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
