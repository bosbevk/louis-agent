using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Git;
using System.Net.Http.Json;

namespace LouisAgent.Web.Workspace;

/// <summary>The code-review app's own API calls: the workspace browser and the git views (Changes, Branches).</summary>
public static class AgentApiWorkspace
{
    public static async Task<WorkspaceFolder> ListFolderAsync(this AgentApi api, string path)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"v1/workspace/entries?path={Uri.EscapeDataString(path)}"));
        return (await response.Content.ReadFromJsonAsync<WorkspaceFolder>())!;
    }

    public static async Task<WorkspaceFile> ReadFileAsync(this AgentApi api, string path)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"v1/workspace/file?path={Uri.EscapeDataString(path)}"));
        return (await response.Content.ReadFromJsonAsync<WorkspaceFile>())!;
    }

    public static async Task<GitStatus> GitStatusAsync(this AgentApi api)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, "v1/git/status"));
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    public static async Task<GitDiff> GitDiffAsync(this AgentApi api, string path, bool staged)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"v1/git/diff?path={Uri.EscapeDataString(path)}&staged={(staged ? "true" : "false")}"));
        return (await response.Content.ReadFromJsonAsync<GitDiff>())!;
    }

    /// <summary>stage | unstage | discard; returns the new status.</summary>
    public static async Task<GitStatus> GitChangeAsync(this AgentApi api, string action, IEnumerable<string> paths)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"v1/git/{action}") { Content = JsonContent.Create(new { paths }) });
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    public static async Task<GitStatus> GitCommitAsync(this AgentApi api, string message)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, "v1/git/commits") { Content = JsonContent.Create(new { message }) });
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    public static Task<GitBranches> GitBranchesAsync(this AgentApi api) => api.GetAsync<GitBranches>("v1/git/branches");

    public static Task<GitLog> GitLogAsync(this AgentApi api, int count = 200) => api.GetAsync<GitLog>($"v1/git/commits?count={count}");

    public static Task<GitCommitDetail> GitShowCommitAsync(this AgentApi api, string sha) => api.GetAsync<GitCommitDetail>($"v1/git/commits/{Uri.EscapeDataString(sha)}");

    public static Task<GitBranchDetail> GitBranchAsync(this AgentApi api, string name) => api.GetAsync<GitBranchDetail>($"v1/git/branches/{BranchPath(name)}");

    /// <summary>Merges the branch into the checked-out one; returns the branches afterwards, with a message saying what happened.</summary>
    public static async Task<GitBranches> GitMergeAsync(this AgentApi api, string branch)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, "v1/git/merge") { Content = JsonContent.Create(new { branch }) });
        return (await response.Content.ReadFromJsonAsync<GitBranches>())!;
    }

    /// <summary>Deletes a merged branch; returns the branches afterwards, with a message saying what happened.</summary>
    public static async Task<GitBranches> GitDeleteBranchAsync(this AgentApi api, string branch)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"v1/git/branches/{BranchPath(branch)}"));
        return (await response.Content.ReadFromJsonAsync<GitBranches>())!;
    }

    // A branch name holds slashes (feature/x): each part is escaped, the slashes stay, as the route's catch-all reads them.
    private static string BranchPath(string name) => string.Join('/', name.Split('/').Select(Uri.EscapeDataString));
}
