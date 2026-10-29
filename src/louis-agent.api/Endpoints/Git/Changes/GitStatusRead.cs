using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Git.Replies;
using LouisAgent.Core.Agent;
using Microsoft.AspNetCore.Http.HttpResults;
using static LouisAgent.Api.Endpoints.Git.GitProcess;
using static LouisAgent.Api.Endpoints.Git.Output.GitOutputParser;

namespace LouisAgent.Api.Endpoints.Git.Changes;

/// <summary>
/// The working tree as the Changes tab shows it: the checked-out branch and every changed file; or the error git gave.
/// Every change answers with it, so the app refreshes in one call.
/// </summary>
internal sealed record GitStatusRead(StatusReply? Reply, JsonHttpResult<ApiError>? Error)
{
    /// <summary>
    /// Asks git for the status and the branch; <paramref name="committed"/> names a commit just made.
    /// </summary>
    public static async Task<GitStatusRead> ReadAsync(AgentEngine engine, string? committed = null)
    {
        var status = await RunAsync(engine, ["status", "--porcelain=v1", "-z", "--untracked-files=all"]);
        if (status.ExitCode != 0) return new GitStatusRead(null, GitError(status));

        var branch = await RunAsync(engine, ["rev-parse", "--abbrev-ref", "HEAD"]);
        string? name = branch.ExitCode == 0 ? branch.Output.Trim() : null;
        return new GitStatusRead(new StatusReply(name, ParseStatus(status.Output), committed), null);
    }
}
