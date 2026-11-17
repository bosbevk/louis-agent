using System.Runtime.CompilerServices;
using LouisAgent.Core.Agent.Turns.Pacing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Time.Testing;

namespace LouisAgent.Core.Tests.Agent.Turns.Pacing;

/// <summary>
/// F155, Louis's pick A of three: a turn's stream, paced on its way out of the engine. The text a turn sends in bursts
/// comes out over time at WordPace's pace, in whole words, and everything else as it is, in the turn's order, once the
/// text before it has gone. Run on a fake clock, so the tests see how much went out by when without waiting.
/// </summary>
public class PacedStreamTests
{
    /// <summary>One frame of the fake clock, as the stream ticks.</summary>
    private static readonly TimeSpan Frame = TimeSpan.FromMilliseconds(33);

    /// <summary>Sixty words of five letters and a space: 300 letters, as one burst would bring them.</summary>
    private static readonly string Burst = string.Concat(Enumerable.Repeat("word ", 60));

    /// <summary>A piece of text the turn sends, as the model streams it.</summary>
    private static ChatResponseUpdate Text(string text) => new(ChatRole.Assistant, text) { MessageId = "m1" };

    /// <summary>A turn that sends these updates, then this error if one is given, as fast as it is asked.</summary>
    private static async IAsyncEnumerable<ChatResponseUpdate> Turn(IEnumerable<ChatResponseUpdate> updates,
        Exception? error = null)
    {
        foreach (var update in updates)
        {
            await Task.Yield();
            yield return update;
        }

        if (error is not null) throw error;
    }

    /// <summary>One update the paced stream handed out, and the fake time it went at.</summary>
    private sealed record Out(ChatResponseUpdate Update, TimeSpan At);

    /// <summary>
    /// Everything the paced stream hands out for this turn, each with its fake time: it asks for the next one and moves
    /// the clock a frame at a time until it comes, so the stream's own timer is what paces it.
    /// </summary>
    private static async Task<List<Out>> DrainAsync(IAsyncEnumerable<ChatResponseUpdate> turn)
    {
        var outs = new List<Out>();
        await DrainIntoAsync(turn, outs);
        return outs;
    }

    /// <summary>The same, into a list the caller holds, so what went out before an error is still there.</summary>
    private static async Task DrainIntoAsync(IAsyncEnumerable<ChatResponseUpdate> turn, List<Out> outs)
    {
        var clock = new FakeTimeProvider();
        var start = clock.GetUtcNow();
        await using var paced = turn.Paced(clock).GetAsyncEnumerator();
        while (true)
        {
            var next = paced.MoveNextAsync().AsTask();
            while (!next.IsCompleted)
            {
                // Real time for the reader to run, then a frame of the fake clock for the stream's timer.
                await Task.Delay(1);
                if (!next.IsCompleted) clock.Advance(Frame);
            }

            if (!await next) return;
            outs.Add(new Out(paced.Current, clock.GetUtcNow() - start));
        }
    }

    /// <summary>All the text handed out, run together.</summary>
    private static string TextOf(IEnumerable<Out> outs) => string.Concat(outs.Select(o => o.Update.Text));

    /// <summary>A burst of 300 letters comes out over time, not at once, and all of it.</summary>
    [Test]
    public async Task ABurst_ComesOutOverTime_NotAtOnce()
    {
        var outs = await DrainAsync(Turn([Text(Burst)]));

        Assert.Multiple(() =>
        {
            Assert.That(outs, Has.Count.GreaterThan(5));
            Assert.That(outs[0].Update.Text.Length, Is.LessThan(60));
            Assert.That(outs[^1].At, Is.GreaterThan(TimeSpan.FromSeconds(0.5)));
            Assert.That(TextOf(outs), Is.EqualTo(Burst));
        });
    }

    /// <summary>Each piece but the last ends on a whole word, never part of one.</summary>
    [Test]
    public async Task EachPiece_EndsOnAWholeWord()
    {
        var outs = await DrainAsync(Turn([Text(Burst.TrimEnd())]));

        Assert.That(outs.SkipLast(1).Select(o => o.Update.Text), Has.All.EndsWith(" "));
    }

    /// <summary>
    /// A tool call waits for the text before it, which goes out whole, and goes out itself as it is; the text after
    /// it comes after; the order is the turn's.
    /// </summary>
    [Test]
    public async Task AToolCall_WaitsForTheTextBeforeIt_AndKeepsTheOrder()
    {
        var call = new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("c1", "ShowPage")]);

        var outs = await DrainAsync(Turn([Text(Burst), call, Text("After it.")]));
        int at = outs.FindIndex(o => ReferenceEquals(o.Update, call));

        Assert.Multiple(() =>
        {
            Assert.That(at, Is.GreaterThan(0));
            Assert.That(TextOf(outs.Take(at)), Is.EqualTo(Burst));
            Assert.That(TextOf(outs.Skip(at + 1)), Is.EqualTo("After it."));
        });
    }

    /// <summary>Once the turn ends, what is left shows at the finishing pace: 200 letters within about 1.3 seconds.</summary>
    [Test]
    public async Task OnceTheTurnEnds_TheRestFinishesSoon()
    {
        string text = string.Concat(Enumerable.Repeat("word ", 40));

        var outs = await DrainAsync(Turn([Text(text)]));

        Assert.That(outs[^1].At, Is.LessThan(TimeSpan.FromSeconds(1.4)));
    }

    /// <summary>An error in the turn reaches the reader after the text before it.</summary>
    [Test]
    public void AnError_ReachesTheReader_AfterTheTextBeforeIt()
    {
        var sent = new List<Out>();

        var error = Assert.ThrowsAsync<InvalidOperationException>(() =>
            DrainIntoAsync(Turn([Text("Some words first. ")], new InvalidOperationException("boom")), sent));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Message, Is.EqualTo("boom"));
            Assert.That(TextOf(sent), Is.EqualTo("Some words first. "));
        });
    }

    /// <summary>A cancelled turn stops the reader, and the turn is let go.</summary>
    [Test]
    public async Task ACancelledTurn_StopsTheReader()
    {
        var turn = new EndlessTurn();
        using var cancel = new CancellationTokenSource();
        await using var paced = turn.Paced(new FakeTimeProvider(), cancel.Token).GetAsyncEnumerator();

        var next = paced.MoveNextAsync().AsTask();
        await turn.Started.Task;
        await cancel.CancelAsync();

        Assert.That(async () => await next, Throws.InstanceOf<OperationCanceledException>());
        await turn.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    /// <summary>A turn that never ends: it says when it has started, and when it has been let go.</summary>
    private sealed class EndlessTurn : IAsyncEnumerable<ChatResponseUpdate>
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>Waits for ever, until it is cancelled.</summary>
        public async IAsyncEnumerator<ChatResponseUpdate> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            try
            {
                Started.TrySetResult();
                await Task.Delay(Timeout.Infinite, cancellationToken);
                yield break;
            }
            finally
            {
                Disposed.TrySetResult();
            }
        }
    }
}
