using LouisAgent.Composition.Logging;
using LouisAgent.Composition.Messages;
using System.Collections.Concurrent;
using System.Text.Json;
using LouisAgent.Acp.Messages;
using LouisAgent.Acp.Messages.Holes.ModelText;
using LouisAgent.Acp.Messages.Holes.UserText;
using LouisAgent.Acp.Paths;
using LouisAgent.Acp.Prompting;
using LouisAgent.Acp.Protocol;
using LouisAgent.Acp.Turns;
using LouisAgent.Core.Agent;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace LouisAgent.Acp;

/// <summary>
/// The Agent Client Protocol server Rider talks to: the loop that reads one JSON-RPC message per line and the dispatch
/// to what each method does (S, CL15: the flow only). The replies go through <see cref="JsonRpcWriter"/>, a prompt's
/// turn through <see cref="PromptHandler"/>, the prompt itself through <see cref="PromptBuilder"/> and the paths in it
/// through <see cref="ContainerPaths"/> and <see cref="GitRoots"/>.
/// </summary>
internal sealed class AcpServer
{
    /// <summary>The server's own lines: a request that failed.</summary>
    private static readonly ILogger Log = AgentLogging.For<AcpServer>();

    /// <summary>The label the server's log lines carry.</summary>
    private const string Tag = "ACP";

    /// <summary>The engine every session shares.</summary>
    private readonly AgentEngine _agent;

    /// <summary>Where Rider's messages come in, one per line.</summary>
    private readonly TextReader _input;

    /// <summary>Where our replies and notifications go out.</summary>
    private readonly JsonRpcWriter _writer;

    /// <summary>What runs a <c>session/prompt</c>.</summary>
    private readonly PromptHandler _prompts;

    /// <summary>The open chats, by session id.</summary>
    private readonly ConcurrentDictionary<string, AcpSession> _sessions = new();

    /// <summary>The requests still running, waited for when Rider closes the line.</summary>
    private readonly ConcurrentBag<Task> _requests = [];

    /// <summary>
    /// Sets the server up on the engine, reading from stdin and writing to stdout unless a test gives others.
    /// </summary>
    /// <param name="agent">The engine every session shares.</param>
    /// <param name="usageInChat">USAGE_IN_CHAT: add each turn's usage line to the chat after the answer (F3).</param>
    /// <param name="input">
    /// The client's messages, one JSON-RPC message per line; stdin in Rider, a script in a test.
    /// </param>
    /// <param name="output">
    /// Where the replies and notifications go; stdout in Rider (the protocol), a buffer in a test.
    /// </param>
    public AcpServer(AgentEngine agent, bool usageInChat = false, TextReader? input = null, TextWriter? output = null)
    {
        _agent = agent;
        _input = input ?? Console.In;
        _writer = new JsonRpcWriter(output ?? Console.Out);
        var paths = ContainerPaths.FromEnvironment();
        var relay = new TurnRelay(agent, _writer);
        _prompts = new PromptHandler(agent, usageInChat, _writer, new PromptBuilder(paths), new GitRoots(paths), relay);
    }

    /// <summary>
    /// Reads Rider's messages until the line closes, and starts each request as it comes; a cancel is handled at once.
    /// </summary>
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
                var notJson = new ParseErrorHoles(ex.Message);
                await _writer.WriteErrorAsync(null, JsonRpcErrorCodes.ParseError,
                    MessageText.Format(AcpUserText.ParseError, notJson), cancellationToken);
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

    /// <summary>
    /// Answers one request by its method; one we don't have, or one that fails, gets a JSON-RPC error back.
    /// </summary>
    private async Task HandleRequestAsync(JsonElement? id, string? method, JsonElement parameters,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (method)
            {
                case "initialize":
                    await InitializeAsync(id, parameters, cancellationToken);
                    break;
                case "session/new":
                    await StartSessionAsync(id, parameters, cancellationToken);
                    break;
                case "session/prompt":
                    await PromptAsync(id, parameters, cancellationToken);
                    break;
                default:
                    var unknown = new MethodNotFoundHoles(method);
                    await _writer.WriteErrorAsync(id, JsonRpcErrorCodes.MethodNotFound,
                        MessageText.Format(AcpUserText.MethodNotFound, unknown), cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Error, AcpLogMessage.RequestFailed, exception: ex, tag: Tag);
            await _writer.WriteErrorAsync(id, JsonRpcErrorCodes.InternalError, ex.Message, cancellationToken);
        }
    }

    /// <summary>Agrees the protocol version and says what this agent can do; another version is refused.</summary>
    private async Task InitializeAsync(JsonElement? id, JsonElement parameters, CancellationToken cancellationToken)
    {
        int requestedVersion = parameters.TryGetProperty("protocolVersion", out JsonElement version)
            ? version.GetInt32()
            : AcpCapabilities.ProtocolVersion;

        if (requestedVersion != AcpCapabilities.ProtocolVersion)
        {
            var asked = new UnsupportedProtocolHoles(requestedVersion);
            await _writer.WriteErrorAsync(id, JsonRpcErrorCodes.InvalidParams,
                MessageText.Format(AcpUserText.UnsupportedProtocol, asked), cancellationToken);
            return;
        }

        await _writer.WriteResultAsync(id, AcpCapabilities.Result, cancellationToken);
    }

    /// <summary>
    /// Opens a chat for Rider's project folder, telling the model where the workspace is mounted, and answers its id.
    /// </summary>
    private async Task StartSessionAsync(JsonElement? id, JsonElement parameters, CancellationToken cancellationToken)
    {
        string cwd = parameters.TryGetProperty("cwd", out JsonElement cwdElement)
            ? cwdElement.GetString() ?? string.Empty
            : string.Empty;
        string sessionId = $"sess_{Guid.NewGuid():N}";
        var history = _agent.StartConversation();
        var workspace = new WorkspaceNoticeHoles(_agent.WorkspaceRoot);
        history.Add(new ChatMessage(ChatRole.System, MessageText.Format(AcpModelText.WorkspaceNotice, workspace)));

        _sessions[sessionId] = new AcpSession(cwd, history, _agent.Display);
        var reply = new NewSessionReply(sessionId);
        await _writer.WriteResultAsync(id, reply, cancellationToken);
    }

    /// <summary>Runs a prompt on the chat it names; a session we don't have is refused.</summary>
    private async Task PromptAsync(JsonElement? id, JsonElement parameters, CancellationToken cancellationToken)
    {
        string sessionId = GetRequiredString(parameters, "sessionId");
        if (!_sessions.TryGetValue(sessionId, out AcpSession? session))
        {
            var unknown = new UnknownSessionHoles(sessionId);
            await _writer.WriteErrorAsync(id, JsonRpcErrorCodes.InvalidParams,
                MessageText.Format(AcpUserText.UnknownSession, unknown), cancellationToken);
            return;
        }

        await _prompts.HandleAsync(id, sessionId, session, parameters, cancellationToken);
    }

    /// <summary>Stops the turn running in the chat named, if there is one.</summary>
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

    /// <summary>A text property the request must carry; without it the request fails with what was missing.</summary>
    private static string GetRequiredString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out JsonElement property) ||
            string.IsNullOrWhiteSpace(property.GetString()))
        {
            var missing = new MissingPropertyHoles(propertyName);
            throw new InvalidOperationException(MessageText.Format(AcpUserText.MissingProperty, missing));
        }

        return property.GetString()!;
    }
}
