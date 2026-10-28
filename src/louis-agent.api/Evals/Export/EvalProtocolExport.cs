using LouisAgent.Api.Evals.Store;
using LouisAgent.Evals.Protocol;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;
using static LouisAgent.Api.Evals.Store.EvalDocuments;

namespace LouisAgent.Api.Evals.Export;

/// <summary>
/// Turns a stored run back into the lines the runner wrote, so you can download it and grade it again. Each line is
/// the contract's event, written by the contract's own writer, so an export reads back with the same reader a run
/// does, time and order included.
/// </summary>
/// <remarks>
/// The store keeps each event's time, so a line gets the time it came with. A run stored without one (cut off before
/// the line came) gets the last time known, starting from the run's start, so the times never go backwards.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0041-json-through-records-by-hand-only-for-bytes.md">ADR 0041: JSON through records</seealso>
public static class EvalProtocolExport
{
    /// <summary>
    /// Walks the run in the store: the first line, then per mode its conversations, their turns and stream events, the
    /// mode's end, and after its last repeat its roll-up.
    /// </summary>
    public static async Task<string> BuildAsync(IEvalStore store, Run run, CancellationToken cancellationToken)
    {
        var events = new List<EvalEvent>();
        var clock = new LastTime(run.BegunAt ?? run.StartedAt);
        DateTimeOffset At(DateTimeOffset? at) => clock.Of(at);

        events.Add(Begin(run, At(run.BegunAt)));
        var conversations = await store.ConversationsAsync(run.Id, cancellationToken);
        foreach (var mode in run.Modes ?? [])
        {
            // One repeat of a mode: its line, then each conversation's turns with the stream events between them.
            var ofMode = conversations.Where(c => c.Key.Mode == mode.Name && c.Key.Repeat == mode.Repeat).ToList();
            events.Add(new ModeEvent(At(mode.At), mode.Name, mode.Repeat, mode.Toolsets, mode.Role, mode.Llm,
                mode.Conversations ?? ofMode.Count));
            foreach (var conversation in ofMode)
            {
                foreach (var turn in await store.TurnsAsync(run.Id, conversation.Key, cancellationToken))
                {
                    events.Add(new QuestionEvent(At(turn.AskedAt), turn.Key.Mode, turn.Key.Repeat, turn.Key.Task,
                        turn.Key.Turn, turn.Session, turn.Question));
                    foreach (var e in await store.TurnEventsAsync(run.Id, turn.Key, cancellationToken))
                        events.Add(new SseEvent(At(e.At), turn.Key.Mode, turn.Key.Repeat, turn.Key.Task,
                            turn.Key.Turn, e.Type, e.Data));
                    events.Add(TurnOf(turn, At(turn.EndedAt)));
                }

                // A conversation that never ended has no result to give back.
                if (ResultOf(conversation, () => At(conversation.At)) is { } result) events.Add(result);
            }

            events.Add(new ModeDoneEvent(At(mode.DoneAt), mode.Name, mode.Repeat, RatesOf(mode.Rates)));

            // After the mode's last repeat, its roll-up as the runner wrote it.
            if (run.Modes!.Last(m => m.Name == mode.Name) == mode)
                events.AddRange((run.Slices ?? []).Where(s => s.Mode == mode.Name).Select(s => s.ToEvent(At(s.At))));
        }

        return string.Concat(events.Select(e => EvalProtocolJson.Line(e) + "\n"));
    }

    /// <summary>The run's first line: its eval, its version and commit, its split, graders and models.</summary>
    private static ProtocolEvent Begin(Run run, DateTimeOffset at) =>
        new(at, ProtocolEvent.CurrentVersion, run.Eval ?? run.Check, run.EvalVersion ?? 0, run.Commit ?? "",
            run.Split ?? default, run.RescoreOf, run.Dataset,
            [.. (run.Graders ?? []).Select(g => new GraderInfo(g.Name, g.Version, g.Kind, g.Question))],
            [.. (run.Models ?? []).Select(m => new ModelInfo(m.Mode, m.Level, m.Role, m.Model))]);

    /// <summary>A finished turn's line: the answer, who answered, its calls, its time and any failure.</summary>
    private static TurnEvent TurnOf(Turn turn, DateTimeOffset at) =>
        new(at, turn.Key.Mode, turn.Key.Repeat, turn.Key.Task, turn.Key.Turn, turn.Session, turn.Question, turn.Answer,
            turn.Role, turn.Continued, turn.AgentCalls, turn.SpecialistCalls, turn.Hosts, turn.Seconds, turn.Error,
            turn.Failure);

    /// <summary>
    /// A conversation's result line, with its grades, outcome and what it measured; none for one that never ended.
    /// </summary>
    private static ConversationEvent? ResultOf(Conversation c, Func<DateTimeOffset> at)
    {
        if (c.Metrics is not { } metrics || c.Outcome is not { } outcome) return null;

        var grades = (c.Grades ?? [])
            .Select(g => new GradeInfo(g.Grader, g.Version, g.Score, g.Outcome, g.Reason, g.JudgeModel,
                g.JudgePromptHash)).ToList();
        return new ConversationEvent(at(), c.Key.Mode, c.Key.Repeat, c.Key.Task, c.Role, c.Errors, c.Seconds, grades,
            outcome, metrics);
    }

    /// <summary>
    /// The time each line is written at: the one it came with, or the last one known when it was stored without one,
    /// so the times never go backwards.
    /// </summary>
    private sealed class LastTime(DateTimeOffset start)
    {
        /// <summary>The last time handed out.</summary>
        private DateTimeOffset _last = start;

        /// <summary>The line's own time if it has one, else the last one; either way it becomes the last.</summary>
        public DateTimeOffset Of(DateTimeOffset? at) => _last = at ?? _last;
    }

    /// <summary>A mode's rates as its end line carries them; none for a repeat the runner wrote no rates for.</summary>
    private static RatesInfo? RatesOf(ModeRates? rates) =>
        rates is { } r ? new RatesInfo(r.PassAtK, r.PassHatK, r.K, r.Infrastructure, r.Retried) : null;
}
