using System.Text.Json;
using LouisAgent.Api.Contracts.Admin;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.LiveRuns;
using LouisAgent.Puls.Admin.Prices;

namespace LouisAgent.Puls.Tests.Admin.LiveRuns;

/// <summary>
/// The table of a run in progress: a row per turn as the script's events arrive, the store's turns for a run picked
/// up again, each turn's cost once the run has ended, and the turn that is open.
/// </summary>
public class LiveTurnsTests
{
    /// <summary>
    /// What the store holds: one finished turn, a scored conversation and an unscored one, and costs.
    /// </summary>
    private static readonly StoredRunReply Stored = JsonSerializer.Deserialize<StoredRunReply>(SampleMode.Fill("""
        {"run":{"id":"r","check":"c","repeat":1,"status":"running","started_at":"2026-10-27T10:00:00+00:00"},
         "modes":[],"notes":[],"agreement":[],
         "turns":[{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1,"question":"Bobotie?","role":"r",
                   "continued":null,"agent_calls":1,"specialist_calls":2,"seconds":3.5,"error":null}],
         "conversations":[{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","role":"r","scores":{"routed_right":true}},
                          {"mode":"<ModeAlpha>","repeat":1,"task":"chat","role":null,"scores":{}}],
         "requests":[
          {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1},"cost":0.01},
          {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"bobotie","turn":1},"cost":0.02},
          {"turn":{"mode":"<ModeAlpha>","repeat":1,"task":"chat","turn":1},"cost":null},
          {"turn":null,"cost":0.04}]}
        """), AdminJson.SnakeCase)!;

    /// <summary>The first turn of bobotie.</summary>
    private static readonly TurnKey Bobotie = new(SampleMode.Alpha, 1, "bobotie", 1);

    /// <summary>A question opens a row and follows it; the stream fills its turn; the turn event finishes it.</summary>
    [Test]
    public void AQuestion_ItsStream_AndItsEnd_FillOneRow()
    {
        var turns = new LiveTurns();

        turns.Apply(Event("""{"event":"question","task":"bobotie","text":"Bobotie?"}"""));
        turns.Apply(Event("""
                          {"event":"sse","task":"bobotie","type":"content_block_start",
                           "data":{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}}
                          """));
        turns.Apply(Event("""
                          {"event":"turn","task":"bobotie","role":null,"continued":"sub-1","puls_calls":1,"specialist_calls":0,
                           "seconds":2.25}
                          """));

        var row = turns.Rows.Single();
        Assert.That(turns.Open, Is.SameAs(row));
        Assert.That((row.Turn.UserText, row.Streamed, row.Done, row.Continued, row.PulsCalls, row.Seconds),
            Is.EqualTo(("Bobotie?", true, true, "sub-1", 1, 2.25)));
    }

    /// <summary>A question for a turn this page already streamed keeps what it streamed.</summary>
    [Test]
    public void AQuestionAgain_KeepsTheStreamedTurn()
    {
        var turns = new LiveTurns();
        turns.Apply(Event("""{"event":"question","task":"bobotie","text":"Bobotie?"}"""));
        turns.Apply(Event("""{"event":"sse","task":"bobotie","type":"ping","data":{"type":"ping"}}"""));
        var turn = turns.Rows.Single().Turn;

        turns.Apply(Event("""{"event":"question","task":"bobotie","text":"Again?"}"""));

        Assert.That(turns.Rows.Single().Turn, Is.SameAs(turn));
    }

    /// <summary>A conversation's scores become the chips under its turns.</summary>
    [Test]
    public void AConversationsScores_AreItsChips()
    {
        var turns = new LiveTurns();

        turns.Apply(Event("""{"event":"conversation","task":"chat","role":null,"scores":{"routed_right":false}}"""));

        Assert.That(turns.ChipsOf("", 1, "chat"), Is.EqualTo(new[] { new PricedChip("Puls itself ✗", "bad") }));
        Assert.That(turns.ChipsOf("", 1, "bobotie"), Is.Null);
    }

    /// <summary>The store's turns are finished rows; a conversation with empty scores has no chips.</summary>
    [Test]
    public void TheStoresTurns_AreFinishedRows()
    {
        var turns = new LiveTurns();

        turns.FromStore(Stored);

        var row = turns.Rows.Single();
        Assert.That((row.Key, row.Turn.UserText, row.Done, row.Streamed, row.Role, row.SpecialistCalls),
            Is.EqualTo((Bobotie, "Bobotie?", true, false, "r", 2)));
        Assert.That(turns.ChipsOf(SampleMode.Alpha, 1, "bobotie"), Has.Count.EqualTo(1));
        Assert.That(turns.ChipsOf(SampleMode.Alpha, 1, "chat"), Is.Null);
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

    /// <summary>A start empties the table, its chips, its costs and the open turn.</summary>
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
        Assert.That(turns.ChipsOf(SampleMode.Alpha, 1, "bobotie"), Is.Null);
    }

    /// <summary>After a start the newest question opens again, even if an older turn was chosen before.</summary>
    [Test]
    public void AStart_FollowsTheNewestQuestionAgain()
    {
        var turns = new LiveTurns();
        turns.Apply(Event("""{"event":"question","task":"a"}"""));
        turns.Apply(Event("""{"event":"question","task":"b"}"""));
        turns.Choose(new TurnKey("", 1, "a", 1));

        turns.Start();
        turns.Apply(Event("""{"event":"question","task":"c"}"""));

        Assert.That(turns.Open!.Task, Is.EqualTo("c"));
    }

    /// <summary>
    /// Choosing an older turn stops the table following the newest question; choosing the newest follows again.
    /// </summary>
    [Test]
    public void ChoosingAnOlderTurn_StopsFollowing()
    {
        var turns = new LiveTurns();
        turns.Apply(Event("""{"event":"question","task":"a","text":"A?"}"""));
        turns.Apply(Event("""{"event":"question","task":"b","text":"B?"}"""));

        var chosen = turns.Choose(new TurnKey("", 1, "a", 1));
        turns.Apply(Event("""{"event":"question","task":"c","text":"C?"}"""));

        Assert.That(chosen!.Task, Is.EqualTo("a"));
        Assert.That(turns.Open!.Task, Is.EqualTo("a"));
        Assert.That(turns.Choose(new TurnKey("", 1, "nothing", 1)), Is.Null);
    }

    /// <summary>The turns grouped by conversation, in the order their first turn came.</summary>
    [Test]
    public void TheTurns_AreGroupedByConversation()
    {
        var turns = new LiveTurns();
        turns.Apply(Event("""{"event":"question","task":"a","turn":1}"""));
        turns.Apply(Event("""{"event":"question","task":"b","turn":1}"""));
        turns.Apply(Event("""{"event":"question","task":"a","turn":2}"""));

        var groups = turns.Conversations().Select(g => (g.Key.Task, g.Count())).ToList();

        Assert.That(groups, Is.EqualTo(new[] { ("a", 2), ("b", 1) }));
    }

    /// <summary>One of the script's events.</summary>
    private static RunEvent Event(string json) => RunEvent.TryRead(json)!;
}
