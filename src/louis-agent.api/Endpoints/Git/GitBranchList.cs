using LouisAgent.Api.Contracts;
using LouisAgent.Api.Contracts.Git.Output;
using LouisAgent.Core.Agent;
using Microsoft.AspNetCore.Http.HttpResults;
using static LouisAgent.Api.Endpoints.Git.GitProcess;
using static LouisAgent.Api.Endpoints.Git.Output.GitOutputParser;

namespace LouisAgent.Api.Endpoints.Git;

/// <summary>
/// The repository's local branches, newest commit first, each with how far it is ahead of and behind the checked-out
/// branch; or the error git gave when it couldn't list them.
/// </summary>
internal sealed record GitBranchList(string? Current, List<GitBranchInfo> Items, JsonHttpResult<ApiError>? Error)
{
    /// <summary>The branch with this name, if there is one.</summary>
    public GitBranchInfo? Find(string? name) => Items.FirstOrDefault(b => b.Name == name);

    /// <summary>Asks git for the checked-out branch and every local branch.</summary>
    public static async Task<GitBranchList> ReadAsync(AgentEngine engine)
    {
        var head = await RunAsync(engine, ["rev-parse", "--abbrev-ref", "HEAD"]);
        string? current = head.ExitCode == 0 ? head.Output.Trim() : null;
        var refs = await RunAsync(engine, [
            "for-each-ref", "refs/heads", "--sort=-committerdate",
            "--format=%(refname:short)%1f%(objectname:short)%1f%(subject)%1f%(authorname)%1f%(committerdate:iso-strict)"
        ]);
        if (refs.ExitCode != 0) return new GitBranchList(current, [], GitError(refs));

        var items = new List<GitBranchInfo>();
        foreach (string line in refs.Output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] f = line.TrimEnd('\r').Split(Field);
            if (f.Length < 5) continue;
            bool isCurrent = f[0] == current;
            var (ahead, behind) = await AheadBehindAsync(engine, current, f[0], isCurrent);
            items.Add(new GitBranchInfo(f[0], f[1], f[2], f[3], f[4], isCurrent, ahead, behind));
        }

        return new GitBranchList(current, items, null);
    }

    /// <summary>
    /// How many commits a branch has that the checked-out one hasn't, and the other way round; none for the checked-out
    /// branch itself, or when nothing is checked out.
    /// </summary>
    private static async Task<(int Ahead, int Behind)> AheadBehindAsync(AgentEngine engine, string? current,
        string name, bool isCurrent)
    {
        if (isCurrent || current is null) return (0, 0);
        // "<behind>\t<ahead>": commits only on HEAD, then commits only on the branch.
        var counts = (await RunAsync(engine, ["rev-list", "--left-right", "--count", $"HEAD...refs/heads/{name}"]))
            .Output.Split('\t');
        if (counts.Length != 2) return (0, 0);

        int.TryParse(counts[0], out int behind);
        int.TryParse(counts[1].Trim(), out int ahead);
        return (ahead, behind);
    }
}
