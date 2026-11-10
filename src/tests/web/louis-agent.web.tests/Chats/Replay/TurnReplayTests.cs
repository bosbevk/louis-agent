using LouisAgent.Web.Chats;
using LouisAgent.Web.Chats.Replay;
using LouisAgent.Web.Testing;

namespace LouisAgent.Web.Tests.Chats.Replay;

/// <summary>
/// The loop that plays stored events into a turn (F120), shared by the eval studio's replay and a recorded session:
/// every event in order, folded the way the live chat folds them, with a redraw now and then and one at the end.
/// </summary>
public class TurnReplayTests
{
    /// <summary>Played without waits, the recorded turn comes out as the live chat would have drawn it.</summary>
    [Test]
    public async Task ARecordedTurn_PlaysIntoTheSameTurn()
    {
        var events = await SessionStreamFixture.EventsAsync();
        var expected = new Turn();
        foreach (var streamEvent in events) TurnStream.Apply(expected, streamEvent);

        var played = new Turn();
        int redraws = 0;
        await TurnReplay.PlayAsync(played, events.Select(e => new ReplayStep(e, 0)).ToList(), () => redraws++,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(played.Blocks.Select(block => (block.Kind, block.Text)),
                Is.EqualTo(expected.Blocks.Select(block => (block.Kind, block.Text))));
            Assert.That(played.StopReason, Is.EqualTo(expected.StopReason));
            Assert.That(redraws, Is.GreaterThan(0).And.LessThan(events.Count),
                "a redraw now and then, not every event");
        });
    }

    /// <summary>
    /// Each tool call is told once, the moment it starts, as the live runner tells it (F127.1): a showroom's mock
    /// website goes where its agent's ShowPage call says.
    /// </summary>
    [Test]
    public async Task EachToolCall_IsToldOnce_AsItStarts()
    {
        var events = await SessionStreamFixture.EventsAsync();
        var expected = new Turn();
        foreach (var streamEvent in events) TurnStream.Apply(expected, streamEvent);
        var told = new List<string?>();

        await TurnReplay.PlayAsync(new Turn(), events.Select(e => new ReplayStep(e, 0)).ToList(), () => { },
            CancellationToken.None, call => told.Add(call.ToolUseId));

        var calls = expected.Blocks.Where(block => block.Kind == BlockKind.Tool).Select(b => b.ToolUseId).ToList();
        Assert.That(calls, Is.Not.Empty, "the recorded turn calls tools");
        Assert.That(told, Is.EqualTo(calls));
    }

    /// <summary>The last event is always drawn, even when it falls between redraws.</summary>
    [Test]
    public async Task TheEnd_IsAlwaysDrawn()
    {
        var events = await SessionStreamFixture.EventsAsync();
        var played = new Turn();
        bool lastDrawSawTheEnd = false;

        await TurnReplay.PlayAsync(played, events.Select(e => new ReplayStep(e, 0)).ToList(),
            () => lastDrawSawTheEnd = played.StopReason is not null, CancellationToken.None);

        Assert.That(lastDrawSawTheEnd, Is.True, "the last draw came after the stream's end was applied");
    }

    /// <summary>Stopped, it stops: a page that goes away doesn't keep playing.</summary>
    [Test]
    public void Stopped_ItStops()
    {
        var steps = new List<ReplayStep>();
        using var stop = new CancellationTokenSource();
        stop.Cancel();

        Assert.That(async () => await TurnReplay.PlayAsync(new Turn(), steps, () => { }, stop.Token),
            Throws.InstanceOf<OperationCanceledException>());
    }
}
