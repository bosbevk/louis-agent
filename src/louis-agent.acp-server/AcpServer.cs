using System.Collections.Concurrent;
using System.Text.Json;
using LouisAgent.AcpServer.Messages;
using LouisAgent.AcpServer.Paths;
using LouisAgent.AcpServer.Prompting;
using LouisAgent.AcpServer.Protocol;
using LouisAgent.AcpServer.Turns;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using LouisAgent.Core.Messages;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LouisAgent.AcpServer;

/// <summary>
/// The Agent Client Protocol server Rider talks to: the loop that reads one JSON-RPC message per line and the dispatch
/// to what each method does (S, CL15: the flow only). The replies go through <see cref="JsonRpcWriter"/>, a prompt's
/// turn through <see cref="PromptHandler"/>, the prompt itself through <see cref="PromptBuilder"/> and the paths in it
/// through <see cref="ContainerPaths"/> and <see cref="GitRoots"/>.
/// </summary>
internal sealed class AcpServer
{
    private static readonly ILogger Log = AgentLogging.For<AcpServer>();
    private const string Tag = "ACP";

    private readonly AgentEngine _agent;
    private readonly TextReader _input;
    private readonly JsonRpcWriter _writer;
    private readonly PromptHandler _prompts;
    private readonly ConcurrentDictionary<string, AcpSession> _sessions = new();
    private readonly ConcurrentBag<Task> _requests = [];

    /// <param name="agent">The engine every session shares.</param>
    /// <param name="usageInChat">USAGE_IN_CHAT: add each turn's usage line to the chat after the answer (F3).</param>
    /// <param name="input">The client's messages, one JSON-RPC message per line; stdin in Rider, a script in a test.</param>
    /// <param name="output">Where the replies and notifications go; stdout in Rider (the protocol), a buffer in a test.</param>
    public AcpServer(AgentEngine agent, bool usageInChat = false, TextReader? input = null, TextWriter? output = null)
    {
        _agent = agent;
        _input = input ?? Console.In;
        _writer = new JsonRpcWriter(output ?? Console.Out);
        var paths = ContainerPaths.FromEnvironment();
        _prompts = new PromptHandler(agent, usageInChat, _writer, new PromptBuilder(paths), new GitRoots(paths), new TurnRelay(agent, _writer));
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await _input.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line);
            }
            catch (JsonException ex)
            {
                await _writer.WriteErrorAsync(null, -32700, MessageText.Format(AcpUserText.ParseError, new { Reason = ex.Message }), cancellationToken);
                continue;
            }

            using (document)
            {
                JsonElement root = document.RootElement;
                string? method = root.TryGetProperty("method", out JsonElement methodElement)
                    ? methodElement.GetString()
                    : null;
                JsonElement? id = root.TryGetProperty("id", out JsonElement idElement)
                    ? idElement.Clone()
                    : null;
                JsonElement parameters = root.TryGetProperty("params", out JsonElement paramsElement)
                    ? paramsElement.Clone()
                    : default;

                if (method == "session/cancel")
                {
                    CancelSession(parameters);
                    continue;
                }

                Task request = HandleRequestAsync(id, method, parameters, cancellationToken);
                _requests.Add(request);
            }
        }

        await Task.WhenAll(_requests.ToArray());
    }

    private async Task HandleRequestAsync(JsonElement? id, string? method, JsonElement parameters, CancellationToken cancellationToken)
    {
        try
        {
            switch (method)
            {
                case "initialize":
                    await InitializeAsync(id, parameters, cancellationToken);
                    break;
                case "session/new":
                    await NewSessionAsync(id, parameters, cancellationToken);
                    break;
                case "session/prompt":
                    await PromptAsync(id, parameters, cancellationToken);
                    break;
                default:
                    await _writer.WriteErrorAsync(id, -32601, MessageText.Format(AcpUserText.MethodNotFound, new { Method = method }), cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Error, AcpLogMessage.RequestFailed, exception: ex, tag: Tag);
            await _writer.WriteErrorAsync(id, -32603, ex.Message, cancellationToken);
        }
    }

    private async Task InitializeAsync(JsonElement? id, JsonElement parameters, CancellationToken cancellationToken)
    {
        int requestedVersion = parameters.TryGetProperty("protocolVersion", out JsonElement version)
            ? version.GetInt32()
            : AcpCapabilities.ProtocolVersion;

        if (requestedVersion != AcpCapabilities.ProtocolVersion)
        {
            await _writer.WriteErrorAsync(id, -32602, MessageText.Format(AcpUserText.UnsupportedProtocol, new { Version = requestedVersion }), cancellationToken);
            return;
        }

        await _writer.WriteResultAsync(id, AcpCapabilities.Result, cancellationToken);
    }

    private async Task NewSessionAsync(JsonElement? id, JsonElement parameters, CancellationToken cancellationToken)
    {
        string cwd = parameters.TryGetProperty("cwd", out JsonElement cwdElement)
            ? cwdElement.GetString() ?? string.Empty
            : string.Empty;
        string sessionId = $"sess_{Guid.NewGuid():N}";
        var history = _agent.NewHistory();
        history.Add(new ChatMessage(ChatRole.System,
            MessageText.Format(AcpModelText.WorkspaceNotice, new { Root = _agent.WorkspaceRoot })));

        _sessions[sessionId] = new AcpSession(cwd, history, _agent.Display);
        await _writer.WriteResultAsync(id, new { sessionId }, cancellationToken);
    }

    private async Task PromptAsync(JsonElement? id, JsonElement parameters, CancellationToken cancellationToken)
    {
        string sessionId = GetRequiredString(parameters, "sessionId");
        if (!_sessions.TryGetValue(sessionId, out AcpSession? session))
        {
            await _writer.WriteErrorAsync(id, -32602, MessageText.Format(AcpUserText.UnknownSession, new { Session = sessionId }), cancellationToken);
            return;
        }

        await _prompts.HandleAsync(id, sessionId, session, parameters, cancellationToken);
    }

    private void CancelSession(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("sessionId", out JsonElement sessionIdElement))
        {
            return;
        }

        string? sessionId = sessionIdElement.GetString();
        if (sessionId is not null && _sessions.TryGetValue(sessionId, out AcpSession? session))
        {
            lock (session.SyncRoot)
            {
                session.ActiveTurn?.Cancel();
            }
        }
    }

    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new InvalidOperationException(MessageText.Format(AcpUserText.MissingProperty, new { Property = propertyName }));
        }

        return property.GetString()!;
    }
}
