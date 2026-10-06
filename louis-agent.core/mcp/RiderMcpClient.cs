namespace louis_agent.core.mcp;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// MCP client for discovering tools from Rider's MCP server.
/// Connects via HTTP to Rider's exposed MCP endpoint (default: http://127.0.0.1:64482).
/// Requires IJ_MCP_SERVER_PROJECT_PATH header for Rider authentication.
/// Uses session-based protocol: initialize first, then tools/list with session ID.
/// </summary>
public sealed class RiderMcpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _endpoint;
    private readonly string? _projectPath;
    private string? _sessionId;
    private const int RequestTimeoutSeconds = 30;

    public RiderMcpClient(string endpoint = "http://127.0.0.1:64482", string? projectPath = null)
    {
        _endpoint = endpoint.TrimEnd('/');
        _projectPath = projectPath ?? Environment.GetEnvironmentVariable("WORKSPACE_ROOT");
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(RequestTimeoutSeconds) };
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(_projectPath))
        {
            request.Headers.Add("IJ_MCP_SERVER_PROJECT_PATH", _projectPath);
        }

        // Include session ID if available
        if (!string.IsNullOrWhiteSpace(_sessionId))
        {
            request.Headers.Add("mcp-session-id", _sessionId);
        }

        // /stream endpoint requires both application/json and text/event-stream in Accept header
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

        return request;
    }

    /// <summary>
    /// Discover tools from Rider's MCP server via HTTP.
    /// Returns empty list if Rider is unavailable (graceful degradation).
    /// Implements MCP bidirectional protocol: SSE stream + JSON-RPC via /stream endpoint.
    /// </summary>
    public async Task<IList<DiscoveredTool>> DiscoverToolsAsync(CancellationToken ct = default)
    {
        try
        {
            // Rider MCP requires initialization before tool discovery
            // Initialize via /stream endpoint, then request tools/list
            return await DiscoverViaStreamAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            Console.Error.WriteLine($"[MCP] Failed to connect to Rider at {_endpoint}: {ex.Message}");
            return [];
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MCP] Tool discovery error: {ex.Message}");
            return [];
        }
    }

    private async Task<List<DiscoveredTool>> DiscoverViaStreamAsync(CancellationToken ct)
    {
        var tools = new List<DiscoveredTool>();

        try
        {
            // Step 1: Send initialize request to establish server state and get session ID
            var initRequest = new
            {
                jsonrpc = "2.0",
                id = 1,
                method = "initialize",
                @params = new
                {
                    protocolVersion = "2024-11-05",
                    capabilities = new { },
                    clientInfo = new
                    {
                        name = "louis-agent",
                        version = "1.0"
                    }
                }
            };

            var initContent = new StringContent(
                JsonSerializer.Serialize(initRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var initHttpRequest = CreateRequest(HttpMethod.Post, $"{_endpoint}/stream");
            initHttpRequest.Content = initContent;
            var initResponse = await _httpClient.SendAsync(initHttpRequest, ct);

            if (!initResponse.IsSuccessStatusCode)
            {
                var errorBody = await initResponse.Content.ReadAsStringAsync(ct);
                Console.Error.WriteLine($"[MCP] Initialize failed: {initResponse.StatusCode}");
                Console.Error.WriteLine($"[MCP] Response: {errorBody}");
                return tools;
            }

            // Extract session ID from response headers
            if (initResponse.Headers.TryGetValues("mcp-session-id", out var sessionValues))
            {
                _sessionId = sessionValues.FirstOrDefault();
                Console.Error.WriteLine($"[MCP] Session established: {_sessionId}");
            }

            // Step 2: Send tools/list request (now with session ID)
            var toolsRequest = new
            {
                jsonrpc = "2.0",
                id = 2,
                method = "tools/list",
                @params = new { }
            };

            var toolsContent = new StringContent(
                JsonSerializer.Serialize(toolsRequest),
                System.Text.Encoding.UTF8,
                "application/json");

            var toolsHttpRequest = CreateRequest(HttpMethod.Post, $"{_endpoint}/stream");
            toolsHttpRequest.Content = toolsContent;
            var toolsResponse = await _httpClient.SendAsync(toolsHttpRequest, ct);
            toolsResponse.EnsureSuccessStatusCode();

            var responseText = await toolsResponse.Content.ReadAsStringAsync(ct);
            var jsonDoc = JsonDocument.Parse(responseText);

            if (jsonDoc.RootElement.TryGetProperty("result", out var resultElement) &&
                resultElement.TryGetProperty("tools", out var toolsElement))
            {
                foreach (var toolJson in toolsElement.EnumerateArray())
                {
                    if (toolJson.TryGetProperty("name", out var nameEl) &&
                        toolJson.TryGetProperty("description", out var descEl))
                    {
                        var name = nameEl.GetString() ?? "unknown";
                        var description = descEl.GetString() ?? "";
                        var inputSchema = toolJson.TryGetProperty("inputSchema", out var schemaEl) ? schemaEl : default;

                        tools.Add(new DiscoveredTool(name, description, inputSchema));
                    }
                }
                Console.Error.WriteLine($"[MCP] Discovered {tools.Count} tools from Rider");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MCP] Stream discovery failed: {ex.Message}");
        }

        return tools;
    }

    private async Task<List<DiscoveredTool>> DiscoverViaSseAsync(CancellationToken ct)
    {
        var tools = new List<DiscoveredTool>();

        try
        {
            // GET /sse to initiate SSE stream
            var httpRequest = CreateRequest(HttpMethod.Get, $"{_endpoint}/sse");
            using var sseResponse = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
            sseResponse.EnsureSuccessStatusCode();

            // Note: Full SSE implementation would require streaming line-by-line parsing
            // For now, we'll attempt a basic response read
            var sseText = await sseResponse.Content.ReadAsStringAsync(ct);
            Console.Error.WriteLine($"[MCP] SSE response (partial): {sseText[..Math.Min(100, sseText.Length)]}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MCP] SSE discovery failed: {ex.Message}");
        }

        return tools;
    }

    public void Dispose() => _httpClient.Dispose();
}

/// <summary>Tool discovered from an MCP server.</summary>
public sealed record DiscoveredTool(
    string Name,
    string Description,
    JsonElement InputSchema);
