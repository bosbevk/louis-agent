using LouisAgent.Api.Evals.Store;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Export;

/// <summary>
/// Turns a stored run back into the lines the harness wrote, so you can download it and score it again. The lines come
/// out in the order the protocol reader expects them.
/// </summary>
/// <remarks>
/// The API doesn't reference the evals project, so it has its own writer that uses the same field names the reader
/// takes. Here the bytes are the contract, so it writes them by hand, ASCII only, as the harness does. The store keeps
/// no clock except each stream event's, so a line's time is the last one known.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class EvalProtocolExport
{
    /// <summary>
    /// Walks the run in the store: the protocol line, then per mode its conversations, their turns and stream events,
    /// and the mode's end.
    /// </summary>
    public static async Task<string> BuildAsync(IEvalStore store, Run run, CancellationToken cancellationToken)
    {
        var lines = new ProtocolLines(run.StartedAt);
        var conversations = await store.ConversationsAsync(run.Id, cancellationToken);
        lines.Protocol(run);

        foreach (var mode in run.Modes ?? [])
        {
            // One repeat of a mode: its line, then each conversation's turns with the stream events between them.
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

            // After the mode's last repeat, its roll-up as the runner wrote it. An older run has none.
            if (run.Modes!.Last(m => m.Name == mode.Name) == mode)
                foreach (var slice in (run.Slices ?? []).Where(s => s.Mode == mode.Name))
                    SummaryExport.Write(lines, slice);
        }

        return lines.ToString();
    }
}
