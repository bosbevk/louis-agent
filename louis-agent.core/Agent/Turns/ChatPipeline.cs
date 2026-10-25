namespace LouisAgent.Core.Agent.Turns;

using LouisAgent.Core.Llm.Replay;
using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Sessions.Replay.Store;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.AI;

/// <summary>
/// The chat client a turn runs on (S, CL15, from AgentEngine): the middleware in its order, the one place to read when a
/// request looks wrong. Outermost first: the tool loop with the guards, the tools kept on the final request, usage recorded
/// (inside the tool loop, so each round is its own request and record), truncated tool calls marked, the request traced,
/// then, innermost and next to the provider, the model's recorded answers where the process keeps a store
/// (<see cref="ModelReplayChatClient"/>, F18 step 4e): below the marking, so a replayed cut-off answer is marked again as the live
/// one was, and below the tracing, so a hit is still a <c>chat</c> span. The summaries' client (<see cref="BuildSummaryClient"/>)
/// skips the pipeline: the provider's client, replayed, traced and recorded the same way.
/// </summary>
internal static class ChatPipeline
{
    /// <param name="concurrentToolCalls">Run a round's tool calls at once (F34); only for toolsets that allow it.</param>
    public static IChatClient Build(IChatClient chatClient, ToolCallGuards guards, IUsageSink? usageSink, IUsageMapper? usageMapper, bool concurrentToolCalls,
        ReplayStore? replay = null)
    {
        var pipeline = new ChatClientBuilder(chatClient)
            .UseFunctionInvocation(configure: c =>
            {
                c.MaximumIterationsPerRequest = 10;
                // F34: a round's calls at once, for an engine whose toolsets all allow it (ToolsetCatalog.RunsInParallel).
                c.AllowConcurrentInvocation = concurrentToolCalls;
                c.FunctionInvoker = guards.InvokeAsync;
                // Lets the model see why a call failed (e.g. a missing argument) instead of a generic failure.
                c.IncludeDetailedErrors = true;
            })
            .Use(guards.KeepToolsOnFinalRequestAsync, guards.KeepToolsOnFinalRequestStreamingAsync);
        // Inside the tool loop, so each round is its own request and record; outside it would see one call per turn.
        if (usageSink is not null)
            pipeline.Use(inner => new UsageRecordingChatClient(inner, usageSink, mapper: usageMapper));
        return pipeline
            .Use(ToolCallGuards.MarkTruncatedToolCallsAsync, ToolCallGuards.MarkTruncatedToolCallsStreamingAsync)
            // Innermost: the span is the request the provider got and its answer, and its duration the provider's alone.
            .UseOpenTelemetry(configure: WithoutContent)
            .Use(inner => replay is null ? inner : new ModelReplayChatClient(inner, replay))
            .Build();
    }

    /// <summary>
    /// Summaries call the model directly, outside the tool loop, so they are traced and recorded by their own layers; the
    /// span is a child of the call it summarises.
    /// </summary>
    public static IChatClient BuildSummaryClient(IChatClient chatClient, IUsageSink? usageSink, IUsageMapper? usageMapper, ReplayStore? replay = null)
    {
        // A summary is a model request too: replayed with the rest, or a free replay would pay for its summaries.
        var traced = new ChatClientBuilder(chatClient).UseOpenTelemetry(configure: WithoutContent)
            .Use(inner => replay is null ? inner : new ModelReplayChatClient(inner, replay)).Build();
        return usageSink is null ? traced : new UsageRecordingChatClient(traced, usageSink, mapper: usageMapper);
    }

    // The default turns on when an operator sets OTEL_INSTRUMENTATION_GENAI_CAPTURE_MESSAGE_CONTENT=true, and a span would
    // then carry a user's words (POPIA). Set explicitly, so no environment can put the conversation in a trace.
    private static void WithoutContent(OpenTelemetryChatClient client) => client.EnableSensitiveData = false;
}
