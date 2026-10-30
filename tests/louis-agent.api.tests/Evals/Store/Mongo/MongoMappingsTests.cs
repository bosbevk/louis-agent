using LouisAgent.Api.Evals.Store.Mongo;
using LouisAgent.Evals.Protocol.Kinds;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace LouisAgent.Api.Tests.Evals.Store.Mongo;

/// <summary>
/// What the Mongo store registers with the driver before its first collection (ADR 0048): an enum is stored as its
/// wire name, never its C# name or a number, and read back from it; a time is stored as a BSON date in UTC and read
/// back at offset zero. No Mongo is needed: the driver's own serializer does the round trip in memory.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0048-mongo-through-records.md">ADR 0048: MongoDB goes through records</seealso>
public class MongoMappingsTests
{
    /// <summary>A document with an enum, an enum that may be missing, and a time.</summary>
    private sealed record Sample(CheckKind Kind, TurnFailure? Failure, DateTimeOffset At);

    /// <summary>Two in the afternoon in Johannesburg, which is noon in UTC.</summary>
    private static readonly DateTimeOffset Afternoon = new(2026, 10, 28, 14, 0, 0, TimeSpan.FromHours(2));

    /// <summary>Registers the mappings, as the store does before its first collection.</summary>
    [SetUp]
    public void Register() => MongoEvalDatabase.RegisterMappings();

    /// <summary>An enum is written as its wire name, and a missing one as a null.</summary>
    [Test]
    public void AnEnum_IsStoredAsItsWireName()
    {
        var stored = new Sample(CheckKind.FixedRule, TurnFailure.ApiError, Afternoon).ToBsonDocument();

        Assert.That((stored["Kind"], stored["Failure"]), Is.EqualTo(((BsonValue)"fixed-rule", (BsonValue)"api-error")));
        Assert.That(new Sample(CheckKind.Model, null, Afternoon).ToBsonDocument()["Failure"],
            Is.EqualTo(BsonNull.Value));
    }

    /// <summary>A time is a BSON date in UTC, and it reads back as the same moment at offset zero.</summary>
    [Test]
    public void ATime_IsAUtcDate_ReadBackAtOffsetZero()
    {
        var stored = new Sample(CheckKind.Model, null, Afternoon).ToBsonDocument();

        var read = BsonSerializer.Deserialize<Sample>(stored);

        Assert.That(stored["At"].ToUniversalTime(), Is.EqualTo(new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc)));
        Assert.That((read.At, read.At.Offset), Is.EqualTo((Afternoon, TimeSpan.Zero)));
    }

    /// <summary>A stored document reads back as the record it was written from.</summary>
    [Test]
    public void ADocument_ReadsBackAsItWasWritten()
    {
        var read = BsonSerializer.Deserialize<Sample>(
            new Sample(CheckKind.Human, TurnFailure.ReplayMiss, Afternoon).ToBsonDocument());

        Assert.That((read.Kind, read.Failure), Is.EqualTo((CheckKind.Human, (TurnFailure?)TurnFailure.ReplayMiss)));
    }

    /// <summary>A stored name that is no member, or a number, is refused with the name it found.</summary>
    [TestCase("nonsense")]
    [TestCase(1)]
    public void ANameThatIsNoMember_IsRefused_ANumberToo(object stored)
    {
        var document = new Sample(CheckKind.Model, null, Afternoon).ToBsonDocument();
        document["Kind"] = BsonValue.Create(stored);

        Assert.That(() => BsonSerializer.Deserialize<Sample>(document),
            Throws.InstanceOf<Exception>().With.Message.Contains(stored.ToString()!));
    }
}
