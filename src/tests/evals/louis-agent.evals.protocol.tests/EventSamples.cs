using System.Text.Json;
using LouisAgent.Evals.Protocol.Events.Conversations;
using LouisAgent.Evals.Protocol.Events.Run;
using LouisAgent.Evals.Protocol.Kinds;
using LouisAgent.Evals.Protocol.Parts.Conversations;
using LouisAgent.Evals.Protocol.Parts.Run;

namespace LouisAgent.Evals.Protocol.Tests;

/// <summary>
/// One of each event, built in code with neutral names, with the nulls and the absent parts the golden file doesn't
/// show, so the round trip covers both.
/// </summary>
internal static class EventSamples
{
    private const string Mode = "sample-mode";
    private const string Task = "sample-task";
    private static readonly DateTimeOffset At = new(2026, 10, 28, 9, 30, 15, 250, TimeSpan.Zero);

    /// <summary>Every event of the protocol, once.</summary>
    public static IEnumerable<EvalEvent> All()
    {
        yield return new ProtocolEvent(At, ProtocolEvent.CurrentVersion, "sample-eval", 1, "unknown", RunSplit.Dev,
            "abc123def456", null, [new GraderInfo("sample-grader", 1, CheckKind.FixedRule, null)],
            [new ModelInfo(Mode, ModelLevel.Grader, null, null)]);
        yield return new ModeEvent(At, Mode, 2, [], null, "sample-llm", null);
        yield return new QuestionEvent(At, Mode, 2, Task, 3, "s-2", "Line one\nline two, \"quoted\"");
        yield return new SseEvent(At, Mode, 2, Task, 3, "tool_use", Data("""{"name":"Find","input":{"n":[1,2]}}"""));
        yield return new TurnEvent(At, Mode, 2, Task, 3, "s-2", "Q?", null, null, "sample-role", 0, 0, [], 0.001,
            "the agent's error", TurnFailure.ReplayMiss);
        yield return new ConversationEvent(At, Mode, 2, Task, null, 1, 0.75, [], TrialOutcome.Infrastructure,
            new Metrics(null, null, null, null, null, null, null, null));
        yield return new ModeDoneEvent(At, Mode, 2, null);
        yield return new SummaryEvent(At, Mode, "budget", 6, 4, 2, 0, 0, [Task], false, 0.6666666666666666, 1, 0.5,
            [new GraderCount("sample-grader", CheckKind.Human, 0, 0, 6, null)], []);
    }

    /// <summary>A stream event's data, kept beyond the document it was parsed from.</summary>
    private static JsonElement Data(string json) => JsonDocument.Parse(json).RootElement.Clone();
}
