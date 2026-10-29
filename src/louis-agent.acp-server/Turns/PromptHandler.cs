
using System.Text.Json;
using LouisAgent.AcpServer.Messages;
using LouisAgent.AcpServer.Paths;
using LouisAgent.AcpServer.Prompting;
using LouisAgent.AcpServer.Protocol;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Logging.Content;
using LouisAgent.Core.Usage.Records;
using Microsoft.Extensions.Logging;

namespace LouisAgent.AcpServer.Turns;
/// <summary>
/// One <c>session/prompt</c> (S, CL15, from AcpServer): the repository the active file is in, the prompt built from the
/// blocks, a user command answered without the model or the turn streamed, its usage recorded, and the stop reason.
/// </summary>
/// <param name="usageInChat">USAGE_IN_CHAT: add each turn's usage line to the chat after the answer (F3).</param>
internal sealed class PromptHandler(
    AgentEngine agent,
    bool usageInChat,
    JsonRpcWriter writer,
    PromptBuilder prompts,
    GitRoots gitRoots,
    TurnRelay relay)
{
    private static readonly ILogger Log = AgentLogging.For<PromptHandler>();
    private const string Tag = "ACP";

    public async Task HandleAsync(JsonElement? id, string sessionId, AcpSession session, JsonElement parameters,
        CancellationToken serverCancellationToken)
    {
        // Extract resource_link (active file in IDE) to detect repository context
        string? resourceLinkPath = PromptBuilder.ResourceLinkPath(parameters);
        Log.Log(LogLevel.Information, AcpLogMessage.ResourceLinkPath, new { Path = resourceLinkPath ?? "(null)" },
            tag: Tag);

        if (!string.IsNullOrEmpty(resourceLinkPath))
        {
            Log.Log(LogLevel.Information, AcpLogMessage.SearchingGitRoot, new { Path = resourceLinkPath }, tag: Tag);
            string gitRoot = gitRoots.Find(resourceLinkPath);
            Log.Log(LogLevel.Information, AcpLogMessage.FindGitRootReturned, new { Root = gitRoot ?? "(empty)" },
                tag: Tag);

            if (!string.IsNullOrEmpty(gitRoot))
            {
                session.WorkingDirectory = gitRoot;
                Log.Log(LogLevel.Information, AcpLogMessage.GitRootDetected, new { Root = gitRoot }, tag: Tag);

                // Set environment variable so git tools use this directory
                Environment.SetEnvironmentVariable("AGENT_WORKING_DIRECTORY", gitRoot);
                Log.Log(LogLevel.Information, AcpLogMessage.WorkingDirectorySet, new { Root = gitRoot }, tag: Tag);
            }
            else
            {
                Log.Log(LogLevel.Warning, AcpLogMessage.NoGitRoot, new { Path = resourceLinkPath }, tag: Tag);
            }
        }
        else
        {
            Log.Log(LogLevel.Information, AcpLogMessage.NoResourceLink, tag: Tag);
            // Clear the working directory override if no resource_link
            Environment.SetEnvironmentVariable("AGENT_WORKING_DIRECTORY", null);
        }

        string prompt = prompts.Build(parameters);
        Log.Log(LogLevel.Information, AcpLogMessage.RawParameters,
            new { Parameters = LogContent.Of(parameters.GetRawText()) }, tag: Tag);
        Log.Log(LogLevel.Information, AcpLogMessage.PromptReceived,
            new { Chars = prompt.Length, Prompt = LogContent.Of(prompt, cut: 300) }, tag: Tag);

        using var turnCancellation = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);

        lock (session.SyncRoot)
        {
            session.ActiveTurn?.Cancel();
            session.ActiveTurn = turnCancellation;
        }

        try
        {
            // /tools, /approve, /reject and /usage typed by the user are answered directly, never by the model.
            if (agent.HandleUserCommand(prompt, agent.RecordsUsage ? session.Usage.Records : null) is { } commandReply)
            {
                await relay.MessageChunkAsync(sessionId, $"msg_{Guid.NewGuid():N}", commandReply,
                    serverCancellationToken);
            }
            else
            {
                // Every model request of this turn is recorded in the usage ledger under this session and turn.
                using var usage = UsageScope.Begin(host: "acp", session: sessionId,
                    turn: Interlocked.Increment(ref session.Turns));
                string? usageLine;
                try
                {
                    await relay.StreamAsync(sessionId, session, prompt, turnCancellation.Token,
                        serverCancellationToken);
                }
                finally
                {
                    // A cancelled or failed turn still counts toward the session: its requests were charged.
                    usageLine = session.Usage.EndTurn(usage.Records);
                    if (usageLine is not null)
                        Log.Log(LogLevel.Information, AcpLogMessage.UsageLine, new { Line = usageLine }, tag: Tag);
                }

                // After a finished answer only: a cancelled turn's reply is "cancelled", with nothing added to it.
                if (usageInChat && usageLine is not null)
                    await relay.MessageChunkAsync(sessionId, $"msg_{Guid.NewGuid():N}", $"\n\n*{usageLine}*",
                        serverCancellationToken);
            }

            await writer.WriteResultAsync(id, new { stopReason = "end_turn" }, serverCancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            Log.Log(LogLevel.Warning, AcpLogMessage.OperationCancelled, new { Reason = ex.Message }, tag: Tag);
            if (ex.InnerException != null)
            {
                Log.Log(LogLevel.Warning, AcpLogMessage.InnerException,
                    new { ErrorType = ex.InnerException.GetType().Name, Reason = ex.InnerException.Message }, tag: Tag);
                Log.Log(LogLevel.Warning, AcpLogMessage.StackTrace, new { Trace = ex.InnerException.StackTrace },
                    tag: Tag);
            }

            await writer.WriteResultAsync(id, new { stopReason = "cancelled" }, serverCancellationToken);
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Error, AcpLogMessage.PromptFailed,
                new { ErrorType = ex.GetType().Name, Reason = ex.Message }, tag: Tag);
            Log.Log(LogLevel.Error, AcpLogMessage.StackTrace, new { Trace = ex.StackTrace }, tag: Tag);
            await writer.WriteErrorAsync(id, -32603, $"{ex.GetType().Name}: {ex.Message}", serverCancellationToken);
        }
        finally
        {
            lock (session.SyncRoot)
            {
                if (ReferenceEquals(session.ActiveTurn, turnCancellation))
                {
                    session.ActiveTurn = null;
                }
            }
        }
    }
}
