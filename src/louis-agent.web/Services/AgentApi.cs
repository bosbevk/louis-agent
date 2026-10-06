using System.Net;
using System.Net.Http.Json;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace louis_agent.web.Services;

/// <summary>Thrown for an API error response; <see cref="Status"/> lets the UI react (401 = key, 404 = session gone).</summary>
public sealed class AgentApiException(HttpStatusCode status, string message) : Exception(message)
{
    public HttpStatusCode Status { get; } = status;
}

/// <summary>One streamed event: its type (content_block_delta, message_stop, ...) and its JSON data.</summary>
public sealed record AgentStreamEvent(string Type, JsonElement Data);

/// <summary>Client for louis-agent.api, which serves this app from the same origin.</summary>
public sealed class AgentApi(HttpClient http, Settings settings)
{
    public async Task<string> CreateSessionAsync()
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "sessions"));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("session_id").GetString()!;
    }

    public async Task CancelAsync(string sessionId)
    {
        using var _ = await SendAsync(new HttpRequestMessage(HttpMethod.Post, $"sessions/{sessionId}/cancel"));
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        try
        {
            using var _ = await SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"sessions/{sessionId}"));
        }
        catch (AgentApiException ex) when (ex.Status == HttpStatusCode.NotFound)
        {
            // Already gone on the server; deleting the local copy is all that's left.
        }
    }

    public async Task<WorkspaceFolder> ListFolderAsync(string path)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, $"workspace/entries?path={Uri.EscapeDataString(path)}"));
        return (await response.Content.ReadFromJsonAsync<WorkspaceFolder>())!;
    }

    public async Task<WorkspaceFile> ReadFileAsync(string path)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, $"workspace/file?path={Uri.EscapeDataString(path)}"));
        return (await response.Content.ReadFromJsonAsync<WorkspaceFile>())!;
    }

    public async Task<GitStatus> GitStatusAsync()
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, "git/status"));
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    public async Task<GitDiff> GitDiffAsync(string path, bool staged)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, $"git/diff?path={Uri.EscapeDataString(path)}&staged={(staged ? "true" : "false")}"));
        return (await response.Content.ReadFromJsonAsync<GitDiff>())!;
    }

    /// <summary>stage | unstage | discard; returns the new status.</summary>
    public async Task<GitStatus> GitChangeAsync(string action, IEnumerable<string> paths)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, $"git/{action}") { Content = JsonContent.Create(new { paths }) });
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    public async Task<GitStatus> GitCommitAsync(string message)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, "git/commit") { Content = JsonContent.Create(new { message }) });
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    public Task<GitBranches> GitBranchesAsync() => GetAsync<GitBranches>("git/branches");

    public Task<GitLog> GitLogAsync(int count = 200) => GetAsync<GitLog>($"git/log?count={count}");

    public Task<GitCommitDetail> GitShowCommitAsync(string sha) => GetAsync<GitCommitDetail>($"git/commit?sha={Uri.EscapeDataString(sha)}");

    public Task<GitBranchDetail> GitBranchAsync(string name) => GetAsync<GitBranchDetail>($"git/branch?name={Uri.EscapeDataString(name)}");

    /// <summary>merge | branch/delete; returns the branches afterwards, with a message saying what happened.</summary>
    public async Task<GitBranches> GitBranchActionAsync(string action, string branch)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Post, $"git/{action}") { Content = JsonContent.Create(new { branch }) });
        return (await response.Content.ReadFromJsonAsync<GitBranches>())!;
    }

    private async Task<T> GetAsync<T>(string url)
    {
        using var response = await SendAsync(new HttpRequestMessage(HttpMethod.Get, url));
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    /// <summary>Sends a message and yields the answer's events as they arrive.</summary>
    public async IAsyncEnumerable<AgentStreamEvent> StreamMessageAsync(
        string sessionId, string message, IReadOnlyList<Attachment> attachments,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"sessions/{sessionId}/messages")
        {
            Content = JsonContent.Create(new
            {
                message,
                attachments = attachments.Select(a => new { name = a.Name, content = a.Content }),
            }),
        };
        request.SetBrowserResponseStreamingEnabled(true);

        using var response = await SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await foreach (var item in SseParser.Create(stream).EnumerateAsync(cancellationToken))
            yield return new AgentStreamEvent(item.EventType, JsonDocument.Parse(item.Data).RootElement);
    }

    private async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        HttpCompletionOption completion = HttpCompletionOption.ResponseContentRead,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(settings.ApiKey)) request.Headers.Add("x-api-key", settings.ApiKey);
        var response = await http.SendAsync(request, completion, cancellationToken);
        if (response.IsSuccessStatusCode) return response;

        string message = response.ReasonPhrase ?? response.StatusCode.ToString();
        try
        {
            var error = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            message = error.GetProperty("error").GetProperty("message").GetString() ?? message;
        }
        catch (Exception)
        {
            // Not the API's JSON error shape; the status text will do.
        }

        var status = response.StatusCode;
        response.Dispose();
        throw new AgentApiException(status, message);
    }
}
