using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Api.Evals.Store.Mongo.Documents;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using LouisAgent.Evals.Protocol.Kinds;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Mongo.Documents;

/// <summary>
/// The Mongo store reads a grader's kind and a model's level back by name, never by number. A stored name it doesn't
/// know is a document this build can't read, and the error names the enum, the value it found and the property it
/// was reading. We spoil the store's own document by hand, so no Mongo is needed.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public class StoredNamesTests
{
    /// <summary>When the run started.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    /// <summary>Registers the store's mappings, as the store does before its first collection.</summary>
    [SetUp]
    public void Register() => MongoEvalDatabase.RegisterMappings();

    /// <summary>A finished run with one grader and one model, as the store writes it to Mongo.</summary>
    private static BsonDocument Stored() => RunDocument.Of(
        new Run("run123456789", "check-x", 1, EvalRunStatus.Done, T0)
        {
            Graders = [new Grader("no-error", 1, GraderKind.FixedRule)],
            Models = [new ModelUse(SampleMode.Alpha, ModelLevel.Agent, null, "model-x")],
        }).ToBsonDocument();

    /// <summary>The run a stored document reads back as.</summary>
    private static Run Read(BsonDocument document) => BsonSerializer.Deserialize<RunDocument>(document).ToRun();

    /// <summary>The names the store writes come back as the same kind and level.</summary>
    [Test]
    public void TheNamesTheStoreWrites_ReadBack()
    {
        var run = Read(Stored());

        Assert.That(run.Graders![0].Kind, Is.EqualTo(GraderKind.FixedRule));
        Assert.That(run.Models![0].Level, Is.EqualTo(ModelLevel.Agent));
    }

    /// <summary>A grader kind stored as a number or an unknown word is refused, naming what it found.</summary>
    [TestCase("1")]
    [TestCase("nonsense")]
    public void AStoredGraderKind_ThatNamesNoMember_IsRefused_ANumberToo(string stored)
    {
        var document = Stored();
        document["graders"][0]["kind"] = stored;

        Assert.That(() => Read(document), Throws.InstanceOf<FormatException>()
            .With.Message.Contains(Refused(nameof(GraderKind), stored)).And.Message.Contains("Kind"));
    }

    /// <summary>A model level stored as a number or an unknown word is refused, naming what it found.</summary>
    [TestCase("1")]
    [TestCase("nonsense")]
    public void AStoredModelLevel_ThatNamesNoMember_IsRefused_ANumberToo(string stored)
    {
        var document = Stored();
        document["models"][0]["level"] = stored;

        Assert.That(() => Read(document), Throws.InstanceOf<FormatException>()
            .With.Message.Contains(Refused(nameof(ModelLevel), stored)).And.Message.Contains("Level"));
    }

    /// <summary>The store's words for a stored name it doesn't know.</summary>
    private static string Refused(string field, string value) =>
        MessageText.Format(ApiUserText.StoredNameUnknown,
            new
            {
                Field = field,
                Value = value
            });
}
