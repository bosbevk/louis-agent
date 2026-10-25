using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using LouisAgent.Api.Benchmarks.Runs;
using LouisAgent.Api.Benchmarks.Store;
using static LouisAgent.Api.Benchmarks.Store.BenchmarkDocuments;

namespace LouisAgent.Api.Benchmarks.Export;

/// <summary>
/// A stored run as the protocol's own lines (F18 step 5.2): <c>@</c> and one JSON object per event, in the order
/// <see cref="BenchmarkProtocol"/> reads them (the protocol line, then for each mode's repeat its mode line, each conversation's
/// question, stream and turn lines, its conversation line, and the mode's end). The API references no evals project, so the writer is its
/// own, with the field names the reader takes; the bytes are the contract here (ADR 0041), ASCII only as the harness writes them. The
/// store keeps no clock but each stream event's, so a line's <c>t</c> is the last time known.
/// </summary>
public static class BenchmarkProtocolExport
{
    public static async Task<string> BuildAsync(IBenchmarkStore store, Run run, CancellationToken cancellationToken)
    {
        var lines = new ProtocolLines(run.StartedAt);
        var conversations = await store.ConversationsAsync(run.Id, cancellationToken);
        lines.Protocol(run);

        foreach (var mode in run.Modes ?? [])
        {
            var ofMode = conversations.Where(c => c.Key.Mode == mode.Name && c.Key.Repeat == mode.Repeat).ToList();
            lines.Mode(mode, ofMode.Count);
            foreach (var conversation in ofMode)
            {
                foreach (var turn in await store.TurnsAsync(run.Id, conversation.Key, cancellationToken))
                {
                    lines.Question(turn);
                    foreach (var streamEvent in await store.TurnEventsAsync(run.Id, turn.Key, cancellationToken))
                        lines.Sse(turn.Key, streamEvent);
                    lines.Turn(turn);
                }
                lines.Conversation(conversation);
            }
            lines.ModeDone(mode);
        }
        return lines.ToString();
    }
}
