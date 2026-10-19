using LouisAgent.Core.Pricing.Rates;
using LouisAgent.Core.Usage.Records;
using System.Collections.Concurrent;
using System.Text.Json;
using LouisAgent.Core.Agent;
using Microsoft.Extensions.AI;
using LouisAgent.Core.Messages;
using LouisAgent.AcpServer.Messages;
using LouisAgent.Core.Logging;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace LouisAgent.AcpServer;

/// <param name="agent">The engine every session shares.</param>
/// <param name="usageInChat">USAGE_IN_CHAT: add each turn's usage line to the chat after the answer (F3).</param>
internal sealed class AcpServer(AgentEngine agent, bool usageInChat = false)
{
    private static readonly ILogger Log = AgentLogging.For<AcpServer>();
    private const string Tag = "ACP";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, SessionState> _sessions = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly ConcurrentBag<Task> _requests = [];

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await Console.In.ReadLineAsync(cancellationToken);
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
                await WriteErrorAsync(null, -32700, MessageText.Format(AcpUserText.ParseError, new { Reason = ex.Message }), cancellationToken);
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

    private async Task HandleRequestAsync(
        JsonElement? id,
        string? method,
        JsonElement parameters,
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
                    await NewSessionAsync(id, parameters, cancellationToken);
                    break;
                case "session/prompt":
                    await PromptAsync(id, parameters, cancellationToken);
                    break;
                default:
                    await WriteErrorAsync(id, -32601, MessageText.Format(AcpUserText.MethodNotFound, new { Method = method }), cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Error, AcpLogMessage.RequestFailed, exception: ex, tag: Tag);
            await WriteErrorAsync(id, -32603, ex.Message, cancellationToken);
        }
    }

    private async Task InitializeAsync(
        JsonElement? id,
        JsonElement parameters,
        CancellationToken cancellationToken)
    {
        int requestedVersion = parameters.TryGetProperty("protocolVersion", out JsonElement version)
            ? version.GetInt32()
            : 1;

        if (requestedVersion != 1)
        {
            await WriteErrorAsync(id, -32602, MessageText.Format(AcpUserText.UnsupportedProtocol, new { Version = requestedVersion }), cancellationToken);
            return;
        }

        await WriteResultAsync(id, new
        {
            protocolVersion = 1,
            agentCapabilities = new
            {
                loadSession = false,
                promptCapabilities = new
                {
                    image = false,
                    audio = false,
                    embeddedContext = true
                },
                mcpCapabilities = new
                {
                    http = false,
                    sse = false
                },
                sessionCapabilities = new { }
            },
            agentInfo = new
            {
                name = "louis-agent",
                title = "Louis Agent",
                version = "1.0.0"
            },
            authMethods = Array.Empty<object>()
        }, cancellationToken);
    }

    private async Task NewSessionAsync(
        JsonElement? id,
        JsonElement parameters,
        CancellationToken cancellationToken)
    {
        string cwd = parameters.TryGetProperty("cwd", out JsonElement cwdElement)
            ? cwdElement.GetString() ?? string.Empty
            : string.Empty;
        string sessionId = $"sess_{Guid.NewGuid():N}";
        var history = agent.NewHistory();
        history.Add(new ChatMessage(ChatRole.System,
            MessageText.Format(AcpModelText.WorkspaceNotice, new { Root = agent.WorkspaceRoot })));

        _sessions[sessionId] = new SessionState(cwd, history, agent.Display);
        await WriteResultAsync(id, new { sessionId }, cancellationToken);
    }

    private async Task PromptAsync(
        JsonElement? id,
        JsonElement parameters,
        CancellationToken serverCancellationToken)
    {
        string sessionId = GetRequiredString(parameters, "sessionId");
        if (!_sessions.TryGetValue(sessionId, out SessionState? session))
        {
            await WriteErrorAsync(id, -32602, MessageText.Format(AcpUserText.UnknownSession, new { Session = sessionId }), serverCancellationToken);
            return;
        }

        // Extract resource_link (active file in IDE) to detect repository context
        string? resourceLinkPath = ExtractResourceLinkPath(parameters);
        Log.Log(LogLevel.Information, AcpLogMessage.ResourceLinkPath, new { Path = resourceLinkPath ?? "(null)" }, tag: Tag);

        if (!string.IsNullOrEmpty(resourceLinkPath))
        {
            Log.Log(LogLevel.Information, AcpLogMessage.SearchingGitRoot, new { Path = resourceLinkPath }, tag: Tag);
            string gitRoot = FindGitRoot(resourceLinkPath);
            Log.Log(LogLevel.Information, AcpLogMessage.FindGitRootReturned, new { Root = gitRoot ?? "(empty)" }, tag: Tag);

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

        string prompt = ExtractPrompt(parameters);
        Log.Log(LogLevel.Information, AcpLogMessage.RawParameters, new { Parameters = parameters.GetRawText() }, tag: Tag);
        Log.Log(LogLevel.Information, AcpLogMessage.PromptReceived, new { Chars = prompt.Length, Prompt = (prompt.Length > 300 ? prompt[..300] + "..." : prompt) }, tag: Tag);

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
                await WriteMessageChunkAsync(sessionId, $"msg_{Guid.NewGuid():N}", commandReply, serverCancellationToken);
            }
            else
            {
                // Every model request of this turn is recorded in the usage ledger under this session and turn.
                using var usage = UsageScope.Begin(host: "acp", session: sessionId, turn: Interlocked.Increment(ref session.Turns));
                string? usageLine;
                try
                {
                    await StreamTurnAsync(sessionId, session, prompt, turnCancellation.Token, serverCancellationToken);
                }
                finally
                {
                    // A cancelled or failed turn still counts toward the session: its requests were charged.
                    usageLine = session.Usage.EndTurn(usage.Records);
                    if (usageLine is not null) Log.Log(LogLevel.Information, AcpLogMessage.UsageLine, new { Line = usageLine }, tag: Tag);
                }

                // After a finished answer only: a cancelled turn's reply is "cancelled", with nothing added to it.
                if (usageInChat && usageLine is not null)
                    await WriteMessageChunkAsync(sessionId, $"msg_{Guid.NewGuid():N}", $"\n\n*{usageLine}*", serverCancellationToken);
            }

            await WriteResultAsync(id, new { stopReason = "end_turn" }, serverCancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            Log.Log(LogLevel.Warning, AcpLogMessage.OperationCancelled, new { Reason = ex.Message }, tag: Tag);
            if (ex.InnerException != null)
            {
                Log.Log(LogLevel.Warning, AcpLogMessage.InnerException, new { ErrorType = ex.InnerException.GetType().Name, Reason = ex.InnerException.Message }, tag: Tag);
                Log.Log(LogLevel.Warning, AcpLogMessage.StackTrace, new { Trace = ex.InnerException.StackTrace }, tag: Tag);
            }
            await WriteResultAsync(id, new { stopReason = "cancelled" }, serverCancellationToken);
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Error, AcpLogMessage.PromptFailed, new { ErrorType = ex.GetType().Name, Reason = ex.Message }, tag: Tag);
            Log.Log(LogLevel.Error, AcpLogMessage.StackTrace, new { Trace = ex.StackTrace }, tag: Tag);
            await WriteErrorAsync(id, -32603, $"{ex.GetType().Name}: {ex.Message}", serverCancellationToken);
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

    /// <summary>
    /// Relays a turn to the client as it happens: the model's thinking (agent_thought_chunk), text as it is written,
    /// and each tool call as a tool_call that is updated to completed/failed when its result arrives.
    /// </summary>
    private async Task StreamTurnAsync(
        string sessionId,
        SessionState session,
        string prompt,
        CancellationToken turnCancellationToken,
        CancellationToken serverCancellationToken)
    {
        // A new message id after each tool call, so text before and after a tool shows as separate messages.
        string messageId = $"msg_{Guid.NewGuid():N}";
        await foreach (var update in agent.StreamPromptAsync(session.History, prompt, turnCancellationToken))
        {
            foreach (var content in update.Contents)
            {
                switch (content)
                {
                    case TextReasoningContent { Text.Length: > 0 } thinking:
                        await WriteNotificationAsync("session/update", new
                        {
                            sessionId,
                            update = new
                            {
                                sessionUpdate = "agent_thought_chunk",
                                content = new { type = "text", text = thinking.Text }
                            }
                        }, serverCancellationToken);
                        break;
                    case TextContent { Text.Length: > 0 } text:
                        await WriteMessageChunkAsync(sessionId, messageId, text.Text, serverCancellationToken);
                        break;
                    case FunctionCallContent call:
                        messageId = $"msg_{Guid.NewGuid():N}";
                        await WriteNotificationAsync("session/update", new
                        {
                            sessionId,
                            update = new
                            {
                                sessionUpdate = "tool_call",
                                toolCallId = call.CallId,
                                title = $"{call.Name} {AgentEngine.DescribeArguments(call.Arguments)}".Trim(),
                                kind = ToolKind(call.Name),
                                status = "in_progress",
                                rawInput = call.Arguments,
                            }
                        }, serverCancellationToken);
                        break;
                    case FunctionResultContent result:
                        string output = result.Result?.ToString() ?? result.Exception?.Message ?? string.Empty;
                        bool failed = result.Exception is not null || output.StartsWith("Error", StringComparison.OrdinalIgnoreCase);
                        await WriteNotificationAsync("session/update", new
                        {
                            sessionId,
                            update = new
                            {
                                sessionUpdate = "tool_call_update",
                                toolCallId = result.CallId,
                                status = failed ? "failed" : "completed",
                                content = new[]
                                {
                                    new
                                    {
                                        type = "content",
                                        content = new
                                        {
                                            type = "text",
                                            text = output.Length > MaxToolOutputPreviewChars
                                                ? output[..MaxToolOutputPreviewChars] + $"\n... [{output.Length - MaxToolOutputPreviewChars:N0} more chars]"
                                                : output
                                        }
                                    }
                                },
                            }
                        }, serverCancellationToken);
                        break;
                }
            }
        }
    }

    // Tool output shown in the client's tool card; the model still receives the full result.
    private const int MaxToolOutputPreviewChars = 2_000;

    /// <summary>ACP tool kind (picks the icon the client shows), guessed from the tool's name.</summary>
    internal static string ToolKind(string toolName) =>
        ToolKindPrefixes.FirstOrDefault(k => toolName.StartsWith(k.Prefix, StringComparison.Ordinal)).Kind ?? "other";

    private static readonly (string Prefix, string Kind)[] ToolKindPrefixes =
    [
        ("Read", "read"), ("Get", "read"), ("List", "read"), ("Show", "read"),
        ("Search", "search"), ("Find", "search"), ("WebSearch", "search"),
        ("Delete", "delete"), ("Rename", "move"),
        ("Write", "edit"), ("Append", "edit"), ("Create", "edit"), ("Copy", "edit"), ("Update", "edit"),
        ("Fetch", "fetch"),
        ("DotNet", "execute"), ("Python", "execute"), ("PowerShell", "execute"), ("Bash", "execute"),
    ];

    private Task WriteMessageChunkAsync(string sessionId, string messageId, string text, CancellationToken cancellationToken) =>
        WriteNotificationAsync("session/update", new
        {
            sessionId,
            update = new
            {
                sessionUpdate = "agent_message_chunk",
                messageId,
                content = new { type = "text", text }
            }
        }, cancellationToken);

    private void CancelSession(JsonElement parameters)
    {
        if (!parameters.TryGetProperty("sessionId", out JsonElement sessionIdElement))
        {
            return;
        }

        string? sessionId = sessionIdElement.GetString();
        if (sessionId is not null && _sessions.TryGetValue(sessionId, out SessionState? session))
        {
            lock (session.SyncRoot)
            {
                session.ActiveTurn?.Cancel();
            }
        }
    }

    /// <summary>
    /// Builds the model's prompt from the ACP content blocks: text, embedded resources (inline file contents) and
    /// files the user attached. Rider sends attachments and the file open in the editor both as resource_link; only
    /// the attached ones are read in, so the open file doesn't get pulled into every prompt.
    /// </summary>
    private static string ExtractPrompt(JsonElement parameters)
    {
        var promptParts = new List<string>();
        if (!parameters.TryGetProperty("prompt", out JsonElement promptItems) || promptItems.ValueKind != JsonValueKind.Array)
            return string.Empty;

        foreach (var item in promptItems.EnumerateArray())
        {
            switch (item.TryGetProperty("type", out JsonElement typeElement) ? typeElement.GetString() : null)
            {
                case "text" when item.TryGetProperty("text", out JsonElement textElement):
                    if (textElement.GetString() is { Length: > 0 } text) promptParts.Add(text);
                    break;

                case "resource" when item.TryGetProperty("resource", out JsonElement resource):
                    string embeddedUri = resource.TryGetProperty("uri", out JsonElement u) ? u.GetString() ?? "" : "";
                    if (resource.TryGetProperty("text", out JsonElement embedded) && embedded.GetString() is { } embeddedText)
                    {
                        Log.Log(LogLevel.Information, AcpLogMessage.EmbeddedResource, new { Uri = embeddedUri, Chars = embeddedText.Length }, tag: Tag);
                        promptParts.Add(FormatAttachment(Path.GetFileName(embeddedUri), embeddedText));
                    }
                    break;

                case "resource_link" when IsAttachedByUser(item):
                    promptParts.Add(ReadAttachedFile(item));
                    break;
            }
        }

        return string.Join("\n\n", promptParts);
    }

    private static bool IsAttachedByUser(JsonElement resourceLink) =>
        !(resourceLink.TryGetProperty("description", out JsonElement description) &&
          (description.GetString() ?? "").Contains("opened in the IDE", StringComparison.OrdinalIgnoreCase));

    // Large enough for any normal source file, small enough that one attachment can't fill the context window.
    private const int MaxAttachmentChars = 100_000;

    private static string ReadAttachedFile(JsonElement resourceLink)
    {
        string uri = resourceLink.TryGetProperty("uri", out JsonElement uriElement) ? uriElement.GetString() ?? "" : "";
        string name = resourceLink.TryGetProperty("name", out JsonElement nameElement) ? nameElement.GetString() ?? "" : "";
        if (string.IsNullOrEmpty(name)) name = Path.GetFileName(uri);

        string? path = uri.StartsWith("file:///") ? ResolveHostPath(Uri.UnescapeDataString(uri[8..])) : null;
        if (path is null)
        {
            Log.Log(LogLevel.Warning, AcpLogMessage.AttachedFileMissing, new { Uri = uri }, tag: Tag);
            return MessageText.Format(AcpModelText.AttachmentNotMounted, new { Name = name, Uri = uri });
        }

        try
        {
            string content = File.ReadAllText(path);
            Log.Log(LogLevel.Information, AcpLogMessage.AttachedFileRead, new { Name = name, Path = path, Chars = content.Length }, tag: Tag);
            return content.Length <= MaxAttachmentChars
                ? FormatAttachment(name, content, path)
                : FormatAttachment(name, content[..MaxAttachmentChars], path) +
                  MessageText.Format(AcpModelText.AttachmentTruncated, new
                  {
                      Shown = MaxAttachmentChars.ToString("N0", CultureInfo.InvariantCulture),
                      Length = content.Length.ToString("N0", CultureInfo.InvariantCulture),
                  });
        }
        catch (Exception ex)
        {
            Log.Log(LogLevel.Warning, AcpLogMessage.AttachedFileReadFailed, new { Path = path, Reason = ex.Message }, tag: Tag);
            return MessageText.Format(AcpModelText.AttachmentUnreadable, new { Name = name, Reason = ex.Message });
        }
    }

    private static string FormatAttachment(string name, string content, string? containerPath = null) =>
        MessageText.Format(containerPath is null ? AcpModelText.Attachment : AcpModelText.AttachmentAt,
            new { Name = name, Path = containerPath, Content = content });

    /// <summary>Maps a host (Windows) path from the IDE to the same file inside the container, or null if it isn't mounted.</summary>
    private static string? ResolveHostPath(string hostPath)
    {
        if (File.Exists(hostPath)) return hostPath;
        return GenerateContainerPathCandidates(hostPath.Replace("\\", "/")).FirstOrDefault(File.Exists);
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

    private Task WriteResultAsync(JsonElement? id, object result, CancellationToken cancellationToken) =>
        WriteAsync(new { jsonrpc = "2.0", id, result }, cancellationToken);

    private Task WriteErrorAsync(
        JsonElement? id,
        int code,
        string message,
        CancellationToken cancellationToken) =>
        WriteAsync(new { jsonrpc = "2.0", id, error = new { code, message } }, cancellationToken);

    private Task WriteNotificationAsync(
        string method,
        object parameters,
        CancellationToken cancellationToken) =>
        WriteAsync(new { jsonrpc = "2.0", method, @params = parameters }, cancellationToken);

    private async Task WriteAsync(object message, CancellationToken cancellationToken)
    {
        string json = JsonSerializer.Serialize(message, JsonOptions);
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            await Console.Out.WriteLineAsync(json.AsMemory(), cancellationToken);
            await Console.Out.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private string? ExtractResourceLinkPath(JsonElement parameters)
    {
        // Extract the active file path from resource_link in the prompt
        if (parameters.TryGetProperty("prompt", out JsonElement promptItems) &&
            promptItems.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in promptItems.EnumerateArray())
            {
                if (item.TryGetProperty("type", out JsonElement typeElement))
                {
                    string itemType = typeElement.GetString() ?? "";
                    if (itemType == "resource_link" && item.TryGetProperty("uri", out JsonElement uriElement))
                    {
                        string uri = uriElement.GetString() ?? "";
                        if (uri.StartsWith("file:///"))
                        {
                            string path = uri.Substring(8); // Remove "file:///"
                            path = System.Uri.UnescapeDataString(path);
                            return path;
                        }
                    }
                }
            }
        }
        return null;
    }

    private string FindGitRoot(string filePath)
    {
        // Normalize path separators
        string workingPath = filePath.Replace("\\", "/");
        Log.Log(LogLevel.Information, AcpLogMessage.FindGitRootNormalized, new { Path = workingPath }, tag: Tag);

        // Try direct path first (works for local dev, not Docker)
        Log.Log(LogLevel.Information, AcpLogMessage.TryingDirectPath, tag: Tag);
        var dirInfo = new DirectoryInfo(Path.GetDirectoryName(workingPath) ?? workingPath);
        while (dirInfo != null)
        {
            string gitPath = Path.Combine(dirInfo.FullName, ".git");
            if (Directory.Exists(gitPath))
            {
                Log.Log(LogLevel.Information, AcpLogMessage.FoundGitDirect, new { Path = dirInfo.FullName }, tag: Tag);
                return dirInfo.FullName;
            }
            Log.Log(LogLevel.Information, AcpLogMessage.NoGitAt, new { Path = dirInfo.FullName }, tag: Tag);
            dirInfo = dirInfo.Parent;
        }

        // Path not found directly - we're in Docker, need to map Windows paths to container paths
        Log.Log(LogLevel.Information, AcpLogMessage.DirectPathExhausted, tag: Tag);
        var containerPaths = GenerateContainerPathCandidates(workingPath);
        Log.Log(LogLevel.Information, AcpLogMessage.CandidatesGenerated, new { containerPaths.Count }, tag: Tag);

        foreach (var containerPath in containerPaths)
        {
            Log.Log(LogLevel.Information, AcpLogMessage.TryingContainerPath, new { Path = containerPath }, tag: Tag);
            dirInfo = new DirectoryInfo(Path.GetDirectoryName(containerPath) ?? containerPath);
            int depth = 0;
            while (dirInfo != null && depth < 10)
            {
                string gitPath = Path.Combine(dirInfo.FullName, ".git");
                Log.Log(LogLevel.Information, AcpLogMessage.CheckingDepth, new { Depth = depth, Path = dirInfo.FullName }, tag: Tag);
                if (Directory.Exists(gitPath))
                {
                    Log.Log(LogLevel.Information, AcpLogMessage.FoundGitMapped, new { Path = dirInfo.FullName }, tag: Tag);
                    return dirInfo.FullName;
                }
                dirInfo = dirInfo.Parent;
                depth++;
            }
        }

        Log.Log(LogLevel.Warning, AcpLogMessage.NoGitFound, tag: Tag);
        return "";
    }

    private static List<string> GenerateContainerPathCandidates(string windowsPath)
    {
        var candidates = new List<string>();

        // Get mount mappings from environment variables (format: HOST_PATH1=/container/path1,HOST_PATH2=/container/path2)
        string? mountMappings = Environment.GetEnvironmentVariable("ACP_MOUNT_MAPPINGS");
        if (!string.IsNullOrWhiteSpace(mountMappings))
        {
            foreach (var mapping in mountMappings.Split(','))
            {
                var parts = mapping.Trim().Split('=');
                if (parts.Length == 2)
                {
                    string hostPath = parts[0].Replace("\\", "/");
                    string containerPath = parts[1];
                    if (windowsPath.StartsWith(hostPath, StringComparison.OrdinalIgnoreCase))
                    {
                        string relativePath = windowsPath.Substring(hostPath.Length);
                        candidates.Add(containerPath + relativePath);
                        Log.Log(LogLevel.Information, AcpLogMessage.CandidateFromMappings, new { Path = containerPath + relativePath }, tag: Tag);
                    }
                }
            }
        }

        // Fallback: try common mount patterns if no explicit mappings provided
        if (candidates.Count == 0)
        {
            // Try /workspace (default for main solution)
            if (Directory.Exists("/workspace"))
            {
                string? workspacePath = FindFileInDirectory("/workspace", windowsPath);
                if (!string.IsNullOrWhiteSpace(workspacePath))
                {
                    candidates.Add(workspacePath);
                }
            }

            // Try /repositories (common for secondary repos)
            if (Directory.Exists("/repositories"))
            {
                string? reposPath = FindFileInDirectory("/repositories", windowsPath);
                if (!string.IsNullOrWhiteSpace(reposPath))
                {
                    candidates.Add(reposPath);
                }
            }

            // Try stripping drive letter and using as-is
            if (windowsPath.Length > 2 && windowsPath[1] == ':')
            {
                string pathWithoutDrive = windowsPath.Substring(2);
                candidates.Add(pathWithoutDrive);
            }
        }

        return candidates;
    }

    private static string? FindFileInDirectory(string containerRoot, string windowsPath)
    {
        // Extract filename from Windows path
        string fileName = Path.GetFileName(windowsPath);
        try
        {
            // Search for the file in the container directory
            var files = Directory.EnumerateFiles(containerRoot, fileName, SearchOption.AllDirectories);
            foreach (var file in files)
            {
                // Verify it's the right file by checking parent directory names
                if (windowsPath.Contains("/"))
                {
                    string[] windowsParts = windowsPath.Split('/');
                    string[] containerParts = file.Replace("\\", "/").Split('/');

                    // Check if last few parts match
                    int matchCount = 0;
                    for (int i = 0; i < Math.Min(3, windowsParts.Length); i++)
                    {
                        int wIdx = windowsParts.Length - 1 - i;
                        int cIdx = containerParts.Length - 1 - i;
                        if (wIdx >= 0 && cIdx >= 0 && windowsParts[wIdx].Equals(containerParts[cIdx], StringComparison.OrdinalIgnoreCase))
                        {
                            matchCount++;
                        }
                    }

                    if (matchCount >= 2)
                    {
                        Log.Log(LogLevel.Information, AcpLogMessage.MatchingFile, new { Root = containerRoot, File = file }, tag: Tag);
                        return file;
                    }
                }
            }
        }
        catch
        {
            // Directory doesn't exist or can't be searched
        }

        return null;
    }

    private sealed class SessionState(string cwd, List<ChatMessage> history, DisplayCurrency? display)
    {
        public string Cwd { get; } = cwd;
        public List<ChatMessage> History { get; } = history;
        public object SyncRoot { get; } = new();
        public CancellationTokenSource? ActiveTurn { get; set; }
        public string WorkingDirectory { get; set; } = cwd;

        /// <summary>Prompts answered by the model so far; the usage ledger's turn number.</summary>
        public int Turns;

        /// <summary>The usage of the session's finished turns, for the usage line's session total and /usage (F3), in the display currency (F14).</summary>
        public UsageSession Usage { get; } = new(display);
    }
}
