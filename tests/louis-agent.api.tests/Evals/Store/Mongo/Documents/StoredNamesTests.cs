using LouisAgent.Api.Evals.Runs.Kinds;
using LouisAgent.Api.Evals.Store.Mongo.Documents;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using MongoDB.Bson;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Mongo.Documents;

/// <summary>
/// The Mongo store reads a grader's kind and a model's level back by name, never by number. A stored name it doesn't
/// know is a document this build can't read, and the error names the field. We spoil the store's own document by
/// hand, so no Mongo is needed.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0024-choices-are-enums.md">ADR 0024: choices are enums</seealso>
public class StoredNamesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    /// <summary>A finished run with one grader and one model, as the store writes it to Mongo.</summary>
    private static BsonDocument Stored() => RunDocuments.ToBson(
        new Run("run123456789", "check-x", 1, EvalRunStatus.Done, T0)
        {
            Graders = [new Grader("no-error", 1, EvalGraderKind.FixedRule)],
            Models = [new ModelUse(SampleMode.Alpha, EvalModelLevel.Agent, null, "model-x")],
        });

    /// <summary>The names the store writes come back as the same kind and level.</summary>
    [Test]
    public void TheNamesTheStoreWrites_ReadBack()
    {
        var run = RunDocuments.RunOf(Stored());

        Assert.That(run.Graders![0].Kind, Is.EqualTo(EvalGraderKind.FixedRule));
        Assert.That(run.Models![0].Level, Is.EqualTo(EvalModelLevel.Agent));
    }

    /// <summary>A grader kind stored as a number or an unknown word is refused, naming the field.</summary>
    [TestCase("1")]
    [TestCase("nonsense")]
    public void AStoredGraderKind_ThatNamesNoMember_IsRefused_ANumberToo(string stored)
    {
        var document = Stored();
        document["graders"][0]["kind"] = stored;

        var error = Assert.Throws<InvalidDataException>(() => RunDocuments.RunOf(document))!;

        Assert.That(error.Message,
            Is.EqualTo(MessageText.Format(ApiUserText.StoredNameUnknown, new
            {
                Field = "kind",
                Value = stored
            })));
    }

    /// <summary>A model level stored as a number or an unknown word is refused, naming the field.</summary>
    [TestCase("1")]
    [TestCase("nonsense")]
    public void AStoredModelLevel_ThatNamesNoMember_IsRefused_ANumberToo(string stored)
    {
        var document = Stored();
        document["models"][0]["level"] = stored;

        var error = Assert.Throws<InvalidDataException>(() => RunDocuments.RunOf(document))!;

        Assert.That(error.Message,
            Is.EqualTo(MessageText.Format(ApiUserText.StoredNameUnknown, new
            {
                Field = "level",
                Value = stored
            })));
    }
}
