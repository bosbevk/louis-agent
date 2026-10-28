using System.Text.Json;
using LouisAgent.Api.Contracts.Admin.Runs;
using LouisAgent.Puls.Admin.LiveRuns;
using LouisAgent.Web.Shared.Chats;

namespace LouisAgent.Puls.Tests.Admin.LiveRuns;

/// <summary>What a turn's row says in its role and seconds cells.</summary>
public class LiveTurnTests
{
    /// <summary>A turn still going says nothing; a finished one names who answered.</summary>
    [Test]
    public void TheRoleCell_SaysWhoAnswered()
    {
        var key = new TurnKey("m", 1, "t", 1);

        Assert.That(new LiveTurn(key).RoleText, Is.EqualTo(""));
        Assert.That(new LiveTurn(key)
        {
            Done = true,
            Role = "r"
        }.RoleText, Is.EqualTo("r"));
        Assert.That(new LiveTurn(key)
        {
            Done = true,
            Continued = "sub-1"
        }.RoleText, Is.EqualTo("continued · sub-1"));
        Assert.That(new LiveTurn(key) { Done = true }.RoleText, Is.EqualTo("Puls itself"));
    }

    /// <summary>The seconds with one decimal, the same on any machine; nothing while the turn goes on.</summary>
    [Test]
    public void TheSecondsCell_HasOneDecimal()
    {
        var key = new TurnKey("m", 1, "t", 1);

        Assert.That(new LiveTurn(key) { Seconds = 4.25 }.SecondsText, Is.EqualTo(""));
        Assert.That(new LiveTurn(key)
        {
            Done = true,
            Seconds = 4.25
        }.SecondsText, Is.EqualTo("4.3"));
    }

    /// <summary>
    /// A turn the page never streamed is replayed from the store's events into a fresh turn with the same question,
    /// and counts as streamed from then on.
    /// </summary>
    [Test]
    public void AReplay_FillsAFreshTurn_WithTheSameQuestion()
    {
        var row = new LiveTurn(new TurnKey("m", 1, "t", 1)) { Turn = new Turn { UserText = "Bobotie?" } };
        var before = row.Turn;
        var events = new[]
        {
            Stored("""{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}"""),
            Stored("""{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hier."}}""")
        };

        row.Replay(events);

        Assert.That(row.Turn, Is.Not.SameAs(before));
        Assert.That((row.Turn.UserText, row.Streamed), Is.EqualTo(("Bobotie?", true)));
        Assert.That(row.Turn.Blocks, Is.Not.Empty);
    }

    /// <summary>One stream event as the store keeps it.</summary>
    private static TurnEventRow Stored(string data)
    {
        var element = JsonDocument.Parse(data).RootElement.Clone();
        return new TurnEventRow(element.GetProperty("type").GetString()!, element, null);
    }
}
