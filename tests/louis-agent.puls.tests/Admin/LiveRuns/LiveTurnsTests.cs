using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Puls.Admin.LiveRuns;
using TurnKey = LouisAgent.Puls.Admin.LiveRuns.TurnKey;

namespace LouisAgent.Puls.Tests.Admin.LiveRuns;

/// <summary>
/// The table of a run in progress: a row per turn as the runner's events arrive (the contract's own records), the
/// store's turns for a run picked up again, each turn's cost once the run has ended, and the turn that is open.
/// </summary>
public class LiveTurnsTests
{
    /// <summary>When the events happened; the table doesn't read it.</summary>
    private static readonly DateTimeOffset T0 = new(2026, 10, 29, 9, 0, 0, TimeSpan.Zero);

    /// <summary>What the store holds: one finished turn of bobotie, and costs.</summary>
    private static readonly StoredRunReply Stored = JsonSerializer.Deserialize<StoredRunReply>(SampleMode.Fill("""
        {"run":{"id":"r","check":"c","repeat":1,"status":"running","started_at":"2026-10-27T10:00:00+00:00"},
         "modes":[],"notes":[],"agreement":[],
         "turns":[{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1,"question":"Bobotie?","role":"r",
                   "continued":null,"agent_calls":1,"specialist_calls":2,"seconds":3.5,"error":null}],
         "conversations":[],
         "requests":[
          {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1},"cost":0.01},
          {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1},"cost":0.02},
          {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"chat","turn":1},"cost":null},
          {"turn":null,"cost":0.04}]}
        """), AdminJson.SnakeCase)!;

    /// <summary>The first turn of bobotie.</summary>
    private static readonly TurnKey Bobotie = new(SampleMode.Alpha, 1, "bobotie", 1);

    /// <summary>A question for a turn of a conversation of the first repeat, in no mode.</summary>
    private static QuestionEvent Question(string task, int turn = 1, string text = "") =>
        new(T0, "", 1, task, turn, "sess", text);

    /// <summary>A piece of a turn's stream.</summary>
    private static SseEvent Sse(string task, string type, string data) =>
        new(T0, "", 1, task, 1, type, JsonDocument.Parse(data).RootElement.Clone());

    /// <summary>A turn's end with its figures.</summary>
    private static TurnEvent Ended(string task, string? continued, int agentCalls, int specialistCalls,
        double seconds) =>
        new(T0, "", 1, task, 1, "sess", "q", null, null, continued, agentCalls, specialistCalls, [], seconds, null,
            null);

    /// <summary>A question opens a row and follows it; the stream fills its turn; the turn's end finishes it.</summary>
    [Test]
    public void AQuestion_ItsStream_AndItsEnd_FillOneRow()
    {
        var turns = new LiveTurns();

        turns.Apply(Question("bobotie", text: "Bobotie?"));
        turns.Apply(Sse("bobotie", "content_block_start",
            """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"""));
        turns.Apply(Ended("bobotie", "sub-1", 1, 0, 2.25));

        var row = turns.Rows.Single();
        Assert.That(turns.Open, Is.SameAs(row));
        Assert.That((row.Turn.UserText, row.Streamed, row.Done, row.Continued, row.AgentCalls, row.Seconds),
            Is.EqualTo(("Bobotie?", true, true, "sub-1", 1, 2.25)));
    }

    /// <summary>A question for a turn this page already streamed keeps what it streamed.</summary>
    [Test]
    public void AQuestionAgain_KeepsTheStreamedTurn()
    {
        var turns = new LiveTurns();
        turns.Apply(Question("bobotie", text: "Bobotie?"));
        turns.Apply(Sse("bobotie", "ping", """{"type":"ping"}"""));
        var turn = turns.Rows.Single().Turn;

        turns.Apply(Question("bobotie", text: "Again?"));

        Assert.That(turns.Rows.Single().Turn, Is.SameAs(turn));
    }

    /// <summary>
    /// A conversation's end adds no row and changes none: its grades are read from the store afterwards.
    /// </summary>
    [Test]
    public void AConversationsEnd_LeavesTheTableAsItIs()
    {
        var turns = new LiveTurns();
        turns.Apply(Question("chat", text: "Hallo"));

        turns.Apply(new ConversationEvent(T0, "", 1, "chat", null, 0, 1.5, [], TrialOutcome.Pass,
            new Metrics(1, 0, [], 1, null, null, 1.5, null)));

        Assert.That(turns.Rows.Select(r => r.Task), Is.EqualTo(new[] { "chat" }));
        Assert.That(turns.Rows.Single().Done, Is.False);
    }

    /// <summary>The store's turns are finished rows.</summary>
    [Test]
    public void TheStoresTurns_AreFinishedRows()
    {
        var turns = new LiveTurns();

        turns.FromStore(Stored);

        var row = turns.Rows.Single();
        Assert.That((row.Key, row.Turn.UserText, row.Done, row.Streamed, row.Role, row.SpecialistCalls),
            Is.EqualTo((Bobotie, "Bobotie?", true, false, "r", 2)));
    }

    /// <summary>Each turn's cost is its requests' known costs summed; a request of no turn counts for none.</summary>
    [Test]
    public void ATurnsCost_IsItsRequestsSummed()
    {
        var turns = new LiveTurns();

        turns.CostsFrom(Stored);

        Assert.That(turns.CostOf(Bobotie), Is.EqualTo(0.03m));
        Assert.That(turns.CostOf(Bobotie with { Task = "chat" }), Is.Null);
        Assert.That(turns.CostOf(Bobotie with { Task = "other" }), Is.Null);
    }

    /// <summary>A start empties the table, its costs and the open turn.</summary>
    [Test]
    public void AStart_EmptiesTheTable()
    {
        var turns = new LiveTurns();
        turns.FromStore(Stored);
        turns.CostsFrom(Stored);
        turns.Choose(Bobotie);

        turns.Start();

        Assert.That(turns.Rows, Is.Empty);
        Assert.That(turns.Open, Is.Null);
        Assert.That(turns.CostOf(Bobotie), Is.Null);
    }

    /// <summary>After a start the newest question opens again, even if an older turn was chosen before.</summary>
    [Test]
    public void AStart_FollowsTheNewestQuestionAgain()
    {
        var turns = new LiveTurns();
        turns.Apply(Question("a"));
        turns.Apply(Question("b"));
        turns.Choose(new TurnKey("", 1, "a", 1));

        turns.Start();
        turns.Apply(Question("c"));

        Assert.That(turns.Open!.Task, Is.EqualTo("c"));
    }

    /// <summary>
    /// Choosing an older turn stops the table following the newest question; choosing the newest follows again.
    /// </summary>
    [Test]
    public void ChoosingAnOlderTurn_StopsFollowing()
    {
        var turns = new LiveTurns();
        turns.Apply(Question("a", text: "A?"));
        turns.Apply(Question("b", text: "B?"));

        var chosen = turns.Choose(new TurnKey("", 1, "a", 1));
        turns.Apply(Question("c", text: "C?"));

        Assert.That(chosen!.Task, Is.EqualTo("a"));
        Assert.That(turns.Open!.Task, Is.EqualTo("a"));
        Assert.That(turns.Choose(new TurnKey("", 1, "nothing", 1)), Is.Null);
    }

    /// <summary>The turns grouped by conversation, in the order their first turn came.</summary>
    [Test]
    public void TheTurns_AreGroupedByConversation()
    {
        var turns = new LiveTurns();
        turns.Apply(Question("a"));
        turns.Apply(Question("b"));
        turns.Apply(Question("a", turn: 2));

        var groups = turns.Conversations().Select(g => (g.Key.Task, g.Count())).ToList();

        Assert.That(groups, Is.EqualTo(new[] { ("a", 2), ("b", 1) }));
    }
}
