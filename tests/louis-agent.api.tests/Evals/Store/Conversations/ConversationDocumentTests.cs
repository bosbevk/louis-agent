using System.Text.Json;
using LouisAgent.Api.Evals.Store.Conversations;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Conversations;

/// <summary>
/// A conversation's Mongo document is a record the driver maps (ADR 0048). It writes exactly what the hand-written
/// mapper before it wrote, so the change moved the code and not the data, and it reads its own document back as the
/// conversation it was. The fixtures are what that mapper wrote for a conversation with every field set and for one
/// that hasn't ended. Stored eval data is disposable until Phase 9, so no document from before is read.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public class ConversationDocumentTests
{
    /// <summary>The run the conversations belong to.</summary>
    private const string Run = "run123456789";

    /// <summary>When the conversation started; whole milliseconds, as a BSON date keeps it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 8, 0, 0, 125, TimeSpan.Zero);

    /// <summary>An ended conversation with every field set: what it measured, a judge's grade and a rule's.</summary>
    private static readonly Conversation Full = new(new ConversationKey(SampleMode.Alpha, 2, "cook-af"), 3,
        SampleRole.Alpha, new Metrics(2, 6, ["www.checkers.co.za", "www.pnp.co.za"], 2, 1200, 340, 12.5, 0.0421m),
        1, 14.25,
        [
            new Grade("meal-for-people", 1, 0.5, GradeOutcome.Fail, "feeds two", "qwen3:8b", "abc123def456"),
            new Grade("no-error", 2, 1, GradeOutcome.Pass, "ok"),
        ],
        TrialOutcome.Fail, T0);

    /// <summary>A conversation that hasn't ended: no role, no metrics, no grades, no outcome, no time.</summary>
    private static readonly Conversation Bare = new(new ConversationKey(SampleMode.Beta, 1, "cook-en"), 1, null, null,
        0, 0);

    /// <summary>A fixture beside this test in the repo, so a regenerated one lands in git.</summary>
    private static string FixturePath(string name) =>
        Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.api.tests", "Evals", "Store", "Conversations",
            "Fixtures", name);

    /// <summary>A fixture as the document it holds, every BSON type kept.</summary>
    private static BsonDocument Fixture(string name) => BsonDocument.Parse(File.ReadAllText(FixturePath(name)));

    /// <summary>The conversation's fields as text, since a record holds its lists by reference.</summary>
    private static string Fields(Conversation conversation) => JsonSerializer.Serialize(conversation);

    /// <summary>Registers the store's mappings, as the store does before its first collection.</summary>
    [SetUp]
    public void Register() => MongoEvalDatabase.RegisterMappings();

    /// <summary>The conversation a fixture holds.</summary>
    private static Conversation Expected(string fixture) => fixture == "conversation-full.json" ? Full : Bare;

    /// <summary>A conversation reads back from its own document as the conversation it was.</summary>
    [TestCase("conversation-full.json")]
    [TestCase("conversation-bare.json")]
    public void AConversation_ReadsBackFromItsOwnDocument(string fixture)
    {
        var conversation = Expected(fixture);
        var written = ConversationDocument.Of(Run, conversation).ToBsonDocument();

        var read = BsonSerializer.Deserialize<ConversationDocument>(written).ToConversation();

        Assert.That(Fields(read), Is.EqualTo(Fields(conversation)));
    }

    /// <summary>
    /// A conversation is written as the old mapper wrote it: the same fields, in the same order, of the same types.
    /// </summary>
    [TestCase("conversation-full.json")]
    [TestCase("conversation-bare.json")]
    public void AConversation_IsWrittenAsTheOldMapperWroteIt(string fixture)
    {
        var conversation = Expected(fixture);

        var written = ConversationDocument.Of(Run, conversation).ToBsonDocument();

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
