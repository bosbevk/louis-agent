using LouisAgent.Api.Evals.Store.Mongo.Documents;
using MongoDB.Bson;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Tests.Evals.Store.Mongo.Documents;

/// <summary>
/// A turn's count of the agent's own calls is kept as <c>agent_calls</c>, a name that belongs to no product. A turn
/// stored before the rename says <c>puls_calls</c>, and it still reads with its count, never as zero.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0037-one-agent-api-products-get-a-front-door.md">ADR 0037: one agent API; products get a front door</seealso>
public class StoredTurnTests
{
    /// <summary>A finished turn where the agent made three calls and its specialists two.</summary>
    private static readonly Turn Asked = new(new TurnKey(SampleMode.Alpha, 1, "bobotie", 1), "s1", "Bobotie?",
        "Hier is bobotie.", null, null, 3, 2, [], 4.5, null);

    /// <summary>A turn is written with the new key, and the old one isn't written at all.</summary>
    [Test]
    public void ATurn_IsWrittenWithAgentCalls()
    {
        var stored = TurnDocuments.ToBson("run1", Asked, []);

        Assert.That(stored["agent_calls"].AsInt32, Is.EqualTo(3));
        Assert.That(stored.Contains("puls_calls"), Is.False);
    }

    /// <summary>What's written reads back as the same turn.</summary>
    [Test]
    public void ATurn_ReadsBackAsItWasWritten() =>
        Assert.That(TurnDocuments.TurnOf(TurnDocuments.ToBson("run1", Asked, [])).AgentCalls, Is.EqualTo(3));

    /// <summary>A turn stored before the rename keeps its count, read from the old key.</summary>
    [Test]
    public void ATurnStoredBeforeTheRename_ReadsItsCountFromTheOldKey()
    {
        // Store the turn as an older API did: the count under puls_calls, and no agent_calls.
        var stored = TurnDocuments.ToBson("run1", Asked, []);
        stored.Remove("agent_calls");
        stored["puls_calls"] = new BsonInt32(3);

        Assert.That(TurnDocuments.TurnOf(stored).AgentCalls, Is.EqualTo(3));
    }
}
