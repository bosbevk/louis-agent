using LouisAgent.Testing;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using LouisAgent.Api.Messages;
using LouisAgent.Core.Messages;
using MongoDB.Bson;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Tests.Benchmarks.Store;

// F18 step 7.5.3: the Mongo store reads a grader's kind and a model's level back with SettingChoice (ADR 0024: a name or its member's name, never a number).
// An unknown stored name is a document the build can't read, and says so with StoredNameUnknown, naming the field. Reads the document the store writes, spoiled by hand, so it needs no Mongo.
public class StoredNamesTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 27, 9, 0, 0, TimeSpan.Zero);

    private static BsonDocument Stored() => MongoBenchmarkStore.ToBson(new Run("run123456789", "check-x", 1, BenchmarkRunStatus.Done, T0)
    {
        Graders = [new Grader("no-error", 1, BenchmarkGraderKind.FixedRule)],
        Models = [new ModelUse(SampleMode.Alpha, BenchmarkModelLevel.Agent, null, "model-x")],
    });

    [Test]
    public void TheNamesTheStoreWrites_ReadBack()
    {
        var run = MongoBenchmarkStore.RunOf(Stored());

        Assert.That(run.Graders![0].Kind, Is.EqualTo(BenchmarkGraderKind.FixedRule));
        Assert.That(run.Models![0].Level, Is.EqualTo(BenchmarkModelLevel.Agent));
    }

    [TestCase("1")]
    [TestCase("nonsense")]
    public void AStoredGraderKind_ThatNamesNoMember_IsRefused_ANumberToo(string stored)
    {
        var document = Stored();
        document["graders"][0]["kind"] = stored;

        var error = Assert.Throws<InvalidDataException>(() => MongoBenchmarkStore.RunOf(document))!;

        Assert.That(error.Message, Is.EqualTo(MessageText.Format(ApiUserText.StoredNameUnknown, new { Field = "kind", Value = stored })));
    }

    [TestCase("1")]
    [TestCase("nonsense")]
    public void AStoredModelLevel_ThatNamesNoMember_IsRefused_ANumberToo(string stored)
    {
        var document = Stored();
        document["models"][0]["level"] = stored;

        var error = Assert.Throws<InvalidDataException>(() => MongoBenchmarkStore.RunOf(document))!;

        Assert.That(error.Message, Is.EqualTo(MessageText.Format(ApiUserText.StoredNameUnknown, new { Field = "level", Value = stored })));
    }
}
