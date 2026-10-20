namespace LouisAgent.Core.Agent.Turns;

using LouisAgent.Core.Llm.UsageMapping;
using LouisAgent.Core.Usage.Sinks;
using Microsoft.Extensions.AI;

/// <summary>
/// The chat client a turn runs on (S, CL15, from AgentEngine): the middleware in its order, the one place to read when a
/// request looks wrong. Outermost first: truncated tool calls marked, usage recorded (inside the tool loop, so each round
/// is its own request and record), the tools kept on the final request, then the tool loop itself with the guards.
/// </summary>
internal static class ChatPipeline
{
    /// <param name="concurrentToolCalls">Run a round's tool calls at once (F34); only for toolsets that allow it.</param>
    public static IChatClient Build(IChatClient chatClient, ToolCallGuards guards, IUsageSink? usageSink, IUsageMapper? usageMapper, bool concurrentToolCalls)
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
            .Build();
    }
}
