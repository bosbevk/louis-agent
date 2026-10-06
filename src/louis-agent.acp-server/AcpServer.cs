using System.Collections.Concurrent;
using System.Text.Json;
using louis_agent.core.tools;
using Microsoft.Extensions.AI;

namespace louis_agent.acp_server;

internal sealed class AcpServer(AgentEngine agent)
{
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
                await WriteErrorAsync(null, -32700, $"Parse error: {ex.Message}", cancellationToken);
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
                    await WriteErrorAsync(id, -32601, $"Method not found: {method}", cancellationToken);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
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
            await WriteErrorAsync(id, -32602, $"Unsupported ACP protocol version: {requestedVersion}", cancellationToken);
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
            $"The Rider project workspace is mounted at '{agent.WorkspaceRoot}'. " +
            "Use the workspace tools to inspect and edit project files. All file paths passed to those tools must be relative to that workspace."));

        _sessions[sessionId] = new SessionState(cwd, history);
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
            await WriteErrorAsync(id, -32602, $"Unknown session: {sessionId}", serverCancellationToken);
            return;
        }

        // Extract resource_link (active file in IDE) to detect repository context
        string? resourceLinkPath = ExtractResourceLinkPath(parameters);
        Console.Error.WriteLine($"[ACP] Resource link path extracted: {resourceLinkPath ?? "(null)"}");

        if (!string.IsNullOrEmpty(resourceLinkPath))
        {
            Console.Error.WriteLine($"[ACP] Searching for git root from: {resourceLinkPath}");
            string gitRoot = FindGitRoot(resourceLinkPath);
            Console.Error.WriteLine($"[ACP] FindGitRoot returned: {gitRoot ?? "(empty)"}");

            if (!string.IsNullOrEmpty(gitRoot))
            {
                session.WorkingDirectory = gitRoot;
                Console.Error.WriteLine($"[ACP] ✓ Detected git root from resource_link: {gitRoot}");

                // Set environment variable so git tools use this directory
                Environment.SetEnvironmentVariable("AGENT_WORKING_DIRECTORY", gitRoot);
                Console.Error.WriteLine($"[ACP] ✓ Set AGENT_WORKING_DIRECTORY to: {gitRoot}");
            }
            else
            {
                Console.Error.WriteLine($"[ACP] ✗ No git root found for: {resourceLinkPath}");
            }
        }
        else
        {
            Console.Error.WriteLine($"[ACP] ✗ No resource_link found in parameters");
            // Clear the working directory override if no resource_link
            Environment.SetEnvironmentVariable("AGENT_WORKING_DIRECTORY", null);
        }

        string prompt = ExtractPrompt(parameters);
        Console.Error.WriteLine($"[ACP] Raw parameters:\n{parameters.GetRawText()}\n");
        Console.Error.WriteLine($"[ACP] Prompt: {prompt.Length} chars: {(prompt.Length > 300 ? prompt[..300] + "..." : prompt)}");

        using var turnCancellation = CancellationTokenSource.CreateLinkedTokenSource(serverCancellationToken);

        lock (session.SyncRoot)
        {
            session.ActiveTurn?.Cancel();
            session.ActiveTurn = turnCancellation;
        }

        try
        {
            // /tools, /approve and /reject typed by the user are answered directly, never by the model.
            if (agent.HandleUserCommand(prompt) is { } commandReply)
            {
                await WriteMessageChunkAsync(sessionId, $"msg_{Guid.NewGuid():N}", commandReply, serverCancellationToken);
            }
            else
            {
                await StreamTurnAsync(sessionId, session, prompt, turnCancellation.Token, serverCancellationToken);
            }

            await WriteResultAsync(id, new { stopReason = "end_turn" }, serverCancellationToken);
        }
        catch (Exception ex) when (ex is OperationCanceledException)
        {
            Console.Error.WriteLine($"[ACP] Agent operation cancelled: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"[ACP] Inner exception: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                Console.Error.WriteLine($"[ACP] Stack trace: {ex.InnerException.StackTrace}");
            }
            await WriteResultAsync(id, new { stopReason = "cancelled" }, serverCancellationToken);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ACP] Unexpected error in PromptAsync: {ex.GetType().Name}: {ex.Message}");
            Console.Error.WriteLine($"[ACP] Stack trace: {ex.StackTrace}");
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
                        Console.Error.WriteLine($"[ACP] Embedded resource {embeddedUri}: {embeddedText.Length} chars");
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
            Console.Error.WriteLine($"[ACP] Attached file not found in the container: {uri}");
            return $"[Attached file: {name} could not be read - it is not inside a folder mounted into the agent ({uri})]";
        }

        try
        {
            string content = File.ReadAllText(path);
            Console.Error.WriteLine($"[ACP] Attached file {name} read from {path}: {content.Length} chars");
            return content.Length <= MaxAttachmentChars
                ? FormatAttachment(name, content, path)
                : FormatAttachment(name, content[..MaxAttachmentChars], path) +
                  $"\n[Attachment truncated: showing {MaxAttachmentChars:N0} of {content.Length:N0} chars]";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ACP] Error reading attached file {path}: {ex.Message}");
            return $"[Attached file: {name} could not be read: {ex.Message}]";
        }
    }

    private static string FormatAttachment(string name, string content, string? containerPath = null) =>
        $"[Attached file: {name}{(containerPath is null ? "" : $" ({containerPath})")}]\n```\n{content}\n```";

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
            throw new InvalidOperationException($"Missing required property '{propertyName}'.");
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
        Console.Error.WriteLine($"[ACP] FindGitRoot: normalized path = {workingPath}");

        // Try direct path first (works for local dev, not Docker)
        Console.Error.WriteLine($"[ACP] Trying direct path...");
        var dirInfo = new DirectoryInfo(Path.GetDirectoryName(workingPath) ?? workingPath);
        while (dirInfo != null)
        {
            string gitPath = Path.Combine(dirInfo.FullName, ".git");
            if (Directory.Exists(gitPath))
            {
                Console.Error.WriteLine($"[ACP]   ✓ Found .git (direct path): {dirInfo.FullName}");
                return dirInfo.FullName;
            }
            Console.Error.WriteLine($"[ACP]   - No .git at: {dirInfo.FullName}");
            dirInfo = dirInfo.Parent;
        }

        // Path not found directly - we're in Docker, need to map Windows paths to container paths
        Console.Error.WriteLine($"[ACP] Direct path search exhausted. Generating container path candidates...");
        var containerPaths = GenerateContainerPathCandidates(workingPath);
        Console.Error.WriteLine($"[ACP] Generated {containerPaths.Count} container path candidates");

        foreach (var containerPath in containerPaths)
        {
            Console.Error.WriteLine($"[ACP] Trying container path: {containerPath}");
            dirInfo = new DirectoryInfo(Path.GetDirectoryName(containerPath) ?? containerPath);
            int depth = 0;
            while (dirInfo != null && depth < 10)
            {
                string gitPath = Path.Combine(dirInfo.FullName, ".git");
                Console.Error.WriteLine($"[ACP]   Depth {depth}: checking {dirInfo.FullName}");
                if (Directory.Exists(gitPath))
                {
                    Console.Error.WriteLine($"[ACP]   ✓ Found .git (mapped path): {dirInfo.FullName}");
                    return dirInfo.FullName;
                }
                dirInfo = dirInfo.Parent;
                depth++;
            }
        }

        Console.Error.WriteLine($"[ACP] ✗ No .git found in any candidate paths");
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
                        Console.Error.WriteLine($"[ACP] Added candidate from ACP_MOUNT_MAPPINGS: {containerPath + relativePath}");
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
                        Console.Error.WriteLine($"[ACP] Found matching file in {containerRoot}: {file}");
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

    private sealed class SessionState(string cwd, List<ChatMessage> history)
    {
        public string Cwd { get; } = cwd;
        public List<ChatMessage> History { get; } = history;
        public object SyncRoot { get; } = new();
        public CancellationTokenSource? ActiveTurn { get; set; }
        public string WorkingDirectory { get; set; } = cwd;
    }
}
