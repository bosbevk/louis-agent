using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Packs.Git.Web.Branches;
using LouisAgent.Packs.Git.Web.Branches.Records;
using LouisAgent.Packs.Git.Web.Views.Branches;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Packs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Git.Web.Views;

/// <summary>Every branch and the commit log as an admin screen: the list beside the open branch or commit.</summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class GitBranchesView : IScreenView
{
    private GitBranchDetail? _branch;
    private GitCommitDetail? _commit;
    private string? _openKey, _error;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private GitHistory History { get; set; } = null!;

    /// <inheritdoc />
    [Parameter] public ScreenReply Screen { get; set; } = null!;

    /// <summary>Whether the open branch is waiting for a merge or a delete.</summary>
    private bool OpenIsBusy => _branch is { } open && History.Pending(open.Branch.Name) is not null;

    /// <summary>Opens a branch; if it can't be read, says why and leaves the list as it is.</summary>
    private Task OpenBranchAsync(string name) => ReadAsync(async () =>
    {
        _branch = await Api.GitBranchAsync(name);
        _commit = null;
        _openKey = BranchesPanel.BranchKey(name);
    });

    /// <summary>Opens a commit; if it can't be read, says why and leaves the list as it is.</summary>
    private Task OpenCommitAsync(string sha) => ReadAsync(async () =>
    {
        _commit = await Api.GitShowCommitAsync(sha);
        _branch = null;
        _openKey = BranchesPanel.CommitKey(sha);
    });

    /// <summary>Merges the branch, and closes it if merging left it nothing to show.</summary>
    private async Task MergeAsync(string name)
    {
        await History.MergeAsync(name);
        var merged = History.Branches?.Branches.FirstOrDefault(b => b.Name == name) is null or { Merged: true };
        Finish(name, merged);
    }

    /// <summary>Deletes the branch, and closes it if it's gone.</summary>
    private async Task DeleteAsync(string name)
    {
        await History.DeleteAsync(name);
        var deleted = History.Branches?.Branches.All(b => b.Name != name) ?? false;
        Finish(name, deleted);
    }

    /// <summary>
    /// An action can wait behind others, so the outcome is judged by this branch alone, and the viewer closes only if it
    /// still shows this branch.
    /// </summary>
    private void Finish(string name, bool done)
    {
        _error = done ? null : History.Error;
        if (done && _branch?.Branch.Name == name) Close();
    }

    /// <summary>Closes what's open, leaving the list.</summary>
    private void Close()
    {
        _branch = null;
        _commit = null;
        _openKey = null;
    }

    /// <summary>Runs a read, keeping what went wrong to show beside the list.</summary>
    private async Task ReadAsync(Func<Task> read)
    {
        _error = null;
        try
        {
            await read();
        }
        catch (AgentApiException ex)
        {
            _error = ex.Message;
        }
        catch (HttpRequestException ex)
        {
            _error = ex.Message;
        }
    }
}
