using System.Net.Http.Json;
using LouisAgent.Packs.Git.Web.Branches.Records;
using LouisAgent.Packs.Git.Web.Changes;
using LouisAgent.Web.Api;

namespace LouisAgent.Packs.Git.Web;

/// <summary>The calls the git views make (Changes, Branches).</summary>
public static class AgentApiGit
{
    /// <summary>What's changed in the workspace.</summary>
    public static Task<GitStatus> GitStatusAsync(this AgentApi api) => api.GetAsync<GitStatus>("v1/git/status");

    /// <summary>One file's changes, staged or not.</summary>
    public static Task<GitDiff> GitDiffAsync(this AgentApi api, string path, bool staged) =>
        api.GetAsync<GitDiff>($"v1/git/diff?path={Uri.EscapeDataString(path)}&staged={(staged ? "true" : "false")}");

    /// <summary>stage | unstage | discard; returns the new status.</summary>
    public static async Task<GitStatus> GitChangeAsync(this AgentApi api, string action, IEnumerable<string> paths)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, $"v1/git/{action}") { Content = JsonContent.Create(new { paths }) });
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    /// <summary>Commits what's staged, and hands back the new status.</summary>
    public static async Task<GitStatus> GitCommitAsync(this AgentApi api, string message)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, "v1/git/commits") { Content = JsonContent.Create(new { message }) });
        return (await response.Content.ReadFromJsonAsync<GitStatus>())!;
    }

    /// <summary>Every branch, and which one is checked out.</summary>
    public static Task<GitBranches> GitBranchesAsync(this AgentApi api) => api.GetAsync<GitBranches>("v1/git/branches");

    /// <summary>The newest commits across all branches.</summary>
    public static Task<GitLog> GitLogAsync(this AgentApi api, int count = 200) => api.GetAsync<GitLog>($"v1/git/commits?count={count}");

    /// <summary>One commit, with its message and diff.</summary>
    public static Task<GitCommitDetail> GitShowCommitAsync(this AgentApi api, string sha) => api.GetAsync<GitCommitDetail>($"v1/git/commits/{Uri.EscapeDataString(sha)}");

    /// <summary>One branch, with what it holds that the checked-out one doesn't.</summary>
    public static Task<GitBranchDetail> GitBranchAsync(this AgentApi api, string name) => api.GetAsync<GitBranchDetail>($"v1/git/branches/{BranchPath(name)}");

    /// <summary>Merges the branch into the checked-out one; returns the branches, and what happened.</summary>
    public static async Task<GitBranches> GitMergeAsync(this AgentApi api, string branch)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Post, "v1/git/merge") { Content = JsonContent.Create(new { branch }) });
        return (await response.Content.ReadFromJsonAsync<GitBranches>())!;
    }

    /// <summary>Deletes a merged branch; returns the branches afterwards, and a message saying what happened.</summary>
    public static async Task<GitBranches> GitDeleteBranchAsync(this AgentApi api, string branch)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"v1/git/branches/{BranchPath(branch)}"));
        return (await response.Content.ReadFromJsonAsync<GitBranches>())!;
    }

    /// <summary>A branch name holds slashes: each part is escaped, and the slashes stay for the route.</summary>
    private static string BranchPath(string name) => string.Join('/', name.Split('/').Select(Uri.EscapeDataString));
}
