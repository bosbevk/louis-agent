using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.Turns.Pacing;

/// <summary>
/// A turn's stream, paced on its way out of the engine (F155): the text the model sends in bursts goes out at a steady
/// pace, in whole words, and everything else as it is, in the turn's order, once the text before it has gone.
/// </summary>
/// <remarks>
/// A Decorator over the turn's stream, Louis's pick A of three: the same updates in the same order, only the text spread
/// over time, so the model call, the tool loop, the usage records and the history run as fast as without it. Chosen over
/// pacing inside the chat pipeline, which would have slowed every tool round and put the pacing in the traces' timings,
/// and over leaving each host to pace, which would leave Rider and the CLI without it. Inside, a producer and a consumer
/// at different speeds with a channel between them, as streamed answers are commonly smoothed: the reader keeps up with
/// the turn so it is never held back, and the writer keeps a reader's pace, catching up when it falls behind.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0008-history-is-append-only.md">ADR 0008: history is append-only</seealso>
public static class PacedStream
{
    /// <summary>How often the writer hands out what the pace allows: about thirty times a second.</summary>
    public static readonly TimeSpan Tick = TimeSpan.FromMilliseconds(33);

    /// <summary>
    /// The turn's updates, the text paced on this clock; an error in the turn comes after the text before it. An
    /// operator on the stream, as <c>WithCancellation</c> is, so a caller reads <c>turn.Paced(clock)</c>.
    /// </summary>
    public static async IAsyncEnumerable<ChatResponseUpdate> Paced(this IAsyncEnumerable<ChatResponseUpdate> turn,
        TimeProvider clock, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var arrivals = Channel.CreateUnbounded<ChatResponseUpdate>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var reader = Task.Run(() => ReadAsync(turn, arrivals.Writer, stop.Token), CancellationToken.None);
        var text = new WaitingText();

        // Something that is not text, held while the text before it finishes, so nothing overtakes it.
        ChatResponseUpdate? held = null;
        try
        {
            using var timer = new PeriodicTimer(Tick, clock);
            long last = clock.GetTimestamp();
            while (true)
            {
                // What the turn has sent so far: text waits its pace; anything else goes at once if no text is
                // before it, or is held, and nothing more is read until it has gone, so the order is the turn's.
                while (held is null && arrivals.Reader.TryRead(out var update))
                {
                    if (WaitingText.IsText(update)) text.Add(update);
                    else if (text.IsEmpty) yield return update;
                    else held = update;
                }

                bool ended = arrivals.Reader.Completion.IsCompleted;
                if (text.IsEmpty && held is not null)
                {
                    yield return held;
                    held = null;
                    continue;
                }

                if (ended && text.IsEmpty) break;

                // Nothing to pace: wait for the turn, and count the pace's time from when it sends more.
                if (text.IsEmpty)
                {
                    await arrivals.Reader.WaitToReadAsync(stop.Token);
                    last = clock.GetTimestamp();
                    continue;
                }

                // The text before something held, or before the turn's end, finishes at the finishing pace.
                await timer.WaitForNextTickAsync(stop.Token);
                long now = clock.GetTimestamp();
                var since = clock.GetElapsedTime(last, now);
                last = now;
                if (text.Take(since, done: ended || held is not null) is { } piece) yield return piece;
            }

            // The turn's own error, if it ended on one, after all the text it sent before it.
            await arrivals.Reader.Completion;
        }
        finally
        {
            await stop.CancelAsync();
            await reader;
        }
    }

    /// <summary>Reads the turn as fast as it sends, into the channel, and closes it with the turn's end or its error.</summary>
    private static async Task ReadAsync(IAsyncEnumerable<ChatResponseUpdate> turn,
        ChannelWriter<ChatResponseUpdate> arrivals, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var update in turn.WithCancellation(cancellationToken))
                arrivals.TryWrite(update);
            arrivals.TryComplete();
        }
        catch (Exception error)
        {
            // Carried to the writer, which raises it after the text before it; a cancelled turn closes the same way.
            arrivals.TryComplete(error);
        }
    }
}
