using System.Net.Http.Json;
using LouisAgent.Web.Shared.Services;

namespace LouisAgent.Web.Services;

/// <summary>The code-review app's own API calls: the workspace browser and the git views (Changes, Branches).</summary>
public static class AgentApiWorkspace
{
    public static async Task<WorkspaceFolder> ListFolderAsync(this AgentApi api, string path)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"workspace/entries?path={Uri.EscapeDataString(path)}"));
        return (await response.Content.ReadFromJsonAsync<WorkspaceFolder>())!;
    }

    public static async Task<WorkspaceFile> ReadFileAsync(this AgentApi api, string path)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"workspace/file?path={Uri.EscapeDataString(path)}"));
        return (await response.Content.ReadFromJsonAsync<WorkspaceFile>())!;
    }

    public static async Task<GitStatus> GitStatusAsync(this AgentApi api)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, "git/status"));
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    public static async Task<GitDiff> GitDiffAsync(this AgentApi api, string path, bool staged)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"git/diff?path={Uri.EscapeDataString(path)}&staged={(staged ? "true" : "false")}"));
        return (await response.Content.ReadFromJsonAsync<GitDiff>())!;
    }

    /// <summary>stage | unstage | discard; returns the new status.</summary>
    public static async Task<GitStatus> GitChangeAsync(this AgentApi api, string action, IEnumerable<string> paths)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"git/{action}") { Content = JsonContent.Create(new { paths }) });
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    public static async Task<GitStatus> GitCommitAsync(this AgentApi api, string message)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, "git/commit") { Content = JsonContent.Create(new { message }) });
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    public static Task<GitBranches> GitBranchesAsync(this AgentApi api) => api.GetAsync<GitBranches>("git/branches");

    public static Task<GitLog> GitLogAsync(this AgentApi api, int count = 200) => api.GetAsync<GitLog>($"git/log?count={count}");

    public static Task<GitCommitDetail> GitShowCommitAsync(this AgentApi api, string sha) => api.GetAsync<GitCommitDetail>($"git/commit?sha={Uri.EscapeDataString(sha)}");

    public static Task<GitBranchDetail> GitBranchAsync(this AgentApi api, string name) => api.GetAsync<GitBranchDetail>($"git/branch?name={Uri.EscapeDataString(name)}");

    /// <summary>merge | branch/delete; returns the branches afterwards, with a message saying what happened.</summary>
    public static async Task<GitBranches> GitBranchActionAsync(this AgentApi api, string action, string branch)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"git/{action}") { Content = JsonContent.Create(new { branch }) });
        return (await response.Content.ReadFromJsonAsync<GitBranches>())!;
    }
}
