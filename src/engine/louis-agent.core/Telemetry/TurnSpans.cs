using System.Diagnostics;
using LouisAgent.Choices;
using LouisAgent.Core.Usage.Records;

namespace LouisAgent.Core.Telemetry;

/// <summary>
/// A turn's span: <c>invoke_agent {agent}</c>, as the OpenTelemetry GenAI conventions name one agent's turn, holding
/// the tool loop's model requests and tool calls, so a trace reads request → turn → calls. It carries the usage
/// scope's ids (the session, host, turn, run and task), never a word of the conversation; a failed turn is an error
/// span with its exception on it.
/// </summary>
/// <remarks>
/// A wrapper at the one place a turn runs (the plan's C1): <c>TurnRunner</c> opens it around both forms of a turn, as a
/// Decorator would, so every host gets it, sub-sessions included, and none opens its own. With nobody listening there
/// is no span at all (the SDK's rule), so a host with no collector pays nothing.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0064-observability-through-opentelemetry.md">ADR 0064: observability through OpenTelemetry</seealso>
public static class TurnSpans
{
    /// <summary>The GenAI operation a turn is.</summary>
    private const string InvokeAgent = "invoke_agent";

    /// <summary>
    /// Opens the turn's span for the agent named, tagged from the usage scope when one is open; none when nobody
    /// listens. A tag the scope doesn't have is left off, never empty.
    /// </summary>
    public static Activity? Start(string agent)
    {
        var span = AgentTelemetry.Source.StartActivity($"{InvokeAgent} {agent}");
        if (span is null) return null;

        span.SetTag(SpanTag.OperationName.Wire(), InvokeAgent);
        span.SetTag(SpanTag.AgentName.Wire(), agent);
        if (UsageScope.Current is not { } scope) return span;

        // A null value leaves the tag off: the scope may know some ids and not others.
        span.SetTag(SpanTag.ConversationId.Wire(), scope.Session);
        span.SetTag(SpanTag.Host.Wire(), scope.Host);
        span.SetTag(SpanTag.Turn.Wire(), scope.Turn);
        span.SetTag(SpanTag.Run.Wire(), scope.Tags.Run);
        span.SetTag(SpanTag.Task.Wire(), scope.Tags.Task);
        return span;
    }

    /// <summary>
    /// Marks the turn as failed and records its exception on the span: its type, message and stack, as the log line
    /// has them. The status has no description, as an exception's message may hold a person's words.
    /// </summary>
    public static void Fail(Activity? span, Exception exception)
    {
        span?.SetStatus(ActivityStatusCode.Error);
        span?.AddException(exception);
    }
}
