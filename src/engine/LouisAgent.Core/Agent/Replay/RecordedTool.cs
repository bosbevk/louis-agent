using System.Text.Json;
using LouisAgent.Composition.Messages;
using LouisAgent.Core.Agent.Messages;
using LouisAgent.Core.Agent.Messages.Holes.Replay;
using LouisAgent.Core.Sessions.Replay;
using LouisAgent.Core.Sessions.Replay.Store;
using Microsoft.Extensions.AI;

namespace LouisAgent.Core.Agent.Replay;

/// <summary>
/// A tool whose answers are recorded on a live run and played back on a free replay, so a replay never reads the
/// world again: a doc that changed since, or a page, can't change what the model is handed. While the session's trial
/// records, it runs the tool and saves what it answered, or the message it failed with, under the tool's name and its
/// arguments. While the trial replays the model too, it answers from the first recording of that call and never runs
/// the tool; a call the recording never saw is a <see cref="ToolReplayMissException"/>. Any other time, a replay with
/// a live model included, the tool runs as it is.
/// </summary>
/// <remarks>
/// Decorator (Gang of Four), on Microsoft.Extensions.AI's own <see cref="DelegatingAIFunction"/>, which hands the
/// tool's name, schema and method through, so the guards and the model see the same tool. It is the shape the web's
/// seams take (<c>RecordedSearchProvider</c>), and Fowler's self-initializing fake, as the model's replay is: one
/// pattern for every seam. Louis's picks (FX235): record tool results, a decorator on each tool rather than a step
/// inside the guards or a subclass of the tool loop, and played back only with the model, as
/// <c>ModelReplayChatClient</c> is, since a recorded answer belongs to the recorded model's call.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0134-an-eval-is-one-file-of-conversations.md">ADR 0134: an eval is one file of conversations</seealso>
public sealed class RecordedTool(AIFunction inner, ReplayStore store) : DelegatingAIFunction(inner)
{
    /// <summary>
    /// The tool, recorded around when the engine keeps recordings. A tool that is only declared, such as one Rider
    /// runs, isn't ours to run, so it stays as it is.
    /// </summary>
    public static AITool Around(AITool tool, ReplayStore? store) =>
        store is not null && tool is AIFunction function ? new RecordedTool(function, store) : tool;

    /// <summary>Records, plays back or just runs the call, by what the session's trial is doing.</summary>
    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments,
        CancellationToken cancellationToken) =>
        ReplayTrial.Current switch
        {
            { Mode: WebReplayMode.Record } trial => await RecordAsync(trial.Of, arguments, cancellationToken),
            { Replaying: true } trial when SessionReplay.Current is { Model: true } =>
                await ReplayAsync(trial.Of, arguments, cancellationToken),
            _ => await base.InvokeCoreAsync(arguments, cancellationToken),
        };

    /// <summary>The call as the store keys it: this tool's name and the arguments the model wrote.</summary>
    private ReplayAsk AskOf(AIFunctionArguments arguments)
    {
        var written = new Dictionary<string, object?>(arguments);
        var key = new ToolKey(Name, JsonSerializer.Serialize(written, AIJsonUtilities.DefaultOptions));
        return ReplayAsk.Of(ReplayKind.Tool, key);
    }

    /// <summary>Runs the tool, saves what it answered or why it failed, and hands the answer on as it is.</summary>
    private async Task<object?> RecordAsync(string trial, AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        ReplayAsk ask = AskOf(arguments);
        object? answer;
        try
        {
            answer = await base.InvokeCoreAsync(arguments, cancellationToken);
        }
        // A cancel isn't the tool's answer, so it isn't kept; any other failure is, by its message, which is what the
        // model is handed.
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            await store.SaveFailureAsync(trial, ask, failure.Message, cancellationToken);
            throw;
        }

        JsonElement saved = JsonSerializer.SerializeToElement(answer, AIJsonUtilities.DefaultOptions);
        await store.SaveAnswerAsync(trial, ask, saved, cancellationToken);
        return answer;
    }

    /// <summary>
    /// The recorded answer to this call, the tool never run, as the JSON it was: Microsoft's tools hand even text back
    /// as JSON, so the model reads the same bytes. A recorded failure is thrown again with its message.
    /// </summary>
    private async Task<object?> ReplayAsync(string trial, AIFunctionArguments arguments,
        CancellationToken cancellationToken)
    {
        if (await store.FindAsync<JsonElement>(trial, AskOf(arguments), cancellationToken) is not { } found)
        {
            var miss = new ToolReplayMissHoles(Name, trial);
            throw new ToolReplayMissException(MessageText.Format(AgentErrorMessage.ToolReplayMiss, miss));
        }

        JsonElement recorded = found.OrThrow(error => new InvalidOperationException(error));
        return recorded.ValueKind == JsonValueKind.Null ? null : recorded;
    }
}
