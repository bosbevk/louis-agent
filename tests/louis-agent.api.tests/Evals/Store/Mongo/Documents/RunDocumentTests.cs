using System.Text.Json;
using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Mongo.Documents;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Run;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Mongo.Documents;

/// <summary>
/// A run's Mongo document is a record the driver maps (ADR 0048), and it writes exactly what the hand-written mapper
/// before it wrote, field for field and type for type, so a run stored before the change opens as it did. The two
/// fixtures are what that mapper wrote for a run with every field set and for a bare one.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public class RunDocumentTests
{
    /// <summary>When the run started; every time is whole milliseconds, as a BSON date keeps it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 28, 8, 0, 0, 125, TimeSpan.Zero);

    /// <summary>A finished run with every field set, its slices and prompts too.</summary>
    internal static readonly Run Full = new("run123456789", SampleMode.Alpha, 2, EvalRunStatus.Done, T0,
        T0.AddMinutes(5), 0, new Dictionary<string, string>
        {
            ["model"] = "claude-haiku-5-5",
            ["fixtures"] = "record"
        },
        [
            new Mode(SampleMode.Alpha, 1, [SampleToolset.Alpha, SampleToolset.Beta], "puls/voice-af", "haiku",
                new ModeRates(1.0, 0.5, 2, 1, 0), 4, T0.AddSeconds(1), T0.AddMinutes(2)),
            new Mode(SampleMode.Beta, 1, [SampleToolset.Beta, SampleToolset.Gamma], "puls/voice-af", null, null, 4,
                T0.AddMinutes(2), null),
        ],
        new Total(0.4217m, "USD", 812_345, 61), ["started", "done"], "a store error",
        Eval: SampleMode.Alpha, EvalVersion: 10, Commit: "abc1234",
        Graders:
        [
            new Grader("routed-to", 1, GraderKind.FixedRule),
            new Grader("meal-for-people.human", 2, GraderKind.Human, "Is this a meal?"),
        ],
        Models: [new ModelUse(SampleMode.Alpha, ModelLevel.Agent, "puls/voice-af", "claude-haiku-5-5")],
        Split: RunSplit.HeldOut, RescoreOf: "old123456789",
        Slices:
        [
            new EvalSlice(SampleMode.Alpha, "overall", 8, 6, 1, 1, 0, ["cook-af", "cook-en"], false, 0.75, 1.0, 0.5,
                [
                    new GraderCount("routed-to", GraderKind.FixedRule, 7, 1, 0, 0.875),
                    new GraderCount("meal-for-people.human", GraderKind.Human, 0, 0, 0, null),
                ],
                [new KindCount(GraderKind.FixedRule, 7, 1, 0)], T0.AddMinutes(4)),
        ],
        Prompts:
        [
            new AgentPrompt(SampleMode.Alpha, "main", ["a1", "b2"]),
            new AgentPrompt(null, "main (grading)", ["j"]),
        ],
        ProtocolVersion: 2, Dataset: new DatasetInfo(SampleMode.Alpha, 6, "3f9c0a1b2d4e"),
        BegunAt: T0.AddSeconds(0.5));

    /// <summary>A run that has only started: every optional field empty.</summary>
    internal static readonly Run Bare = new("run000000001", "stub", 1, EvalRunStatus.Running, T0);

    /// <summary>A fixture beside this test in the repo, so a regenerated one lands in git.</summary>
    private static string FixturePath(string name) =>
        Path.Combine(ApiTestHost.RepoRoot, "tests", "louis-agent.api.tests", "Evals", "Store", "Mongo", "Documents",
            "Fixtures", name);

    /// <summary>A fixture as the document it holds, every BSON type kept.</summary>
    private static BsonDocument Fixture(string name) => BsonDocument.Parse(File.ReadAllText(FixturePath(name)));

    /// <summary>The run's fields as text, since a record holds its lists by reference.</summary>
    private static string Fields(Run run) => JsonSerializer.Serialize(run);

    /// <summary>Registers the store's mappings, as the store does before its first collection.</summary>
    [SetUp]
    public void Register() => MongoEvalDatabase.RegisterMappings();

    /// <summary>
    /// A run stored by the old mapper reads back as the run it was; a bare run's modes come back as an empty list, as
    /// they always did.
    /// </summary>
    [TestCase("run-full.json")]
    [TestCase("run-bare.json")]
    public void ARunStoredBeforeTheRecord_ReadsBackAsItWas(string fixture)
    {
        var expected = fixture == "run-full.json" ? Full : Bare with { Modes = [] };

        var read = BsonSerializer.Deserialize<RunDocument>(Fixture(fixture)).ToRun();

        Assert.That(Fields(read), Is.EqualTo(Fields(expected)));
    }

    /// <summary>
    /// A run is written as the old mapper wrote it: the same fields, in the same order, of the same types.
    /// </summary>
    [TestCase("run-full.json")]
    [TestCase("run-bare.json")]
    public void ARun_IsWrittenAsTheOldMapperWroteIt(string fixture)
    {
        var run = fixture == "run-full.json" ? Full : Bare;

        var written = RunDocument.Of(run).ToBsonDocument();

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
