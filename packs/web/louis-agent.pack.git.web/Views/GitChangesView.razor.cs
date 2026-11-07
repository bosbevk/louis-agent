using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Packs.Git.Web.Changes;
using LouisAgent.Packs.Git.Web.Views.Changes;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Packs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Git.Web.Views;

/// <summary>
/// What's changed in the workspace as an admin screen: the list beside the open diff. A review hands the chat a draft
/// and sends nothing, so the person reads the prompt first.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class GitChangesView : IScreenView
{
    private GitDiff? _diff;
    private string? _openKey, _error;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private GitState Git { get; set; } = null!;
    [Inject] private ChatHandoff Handoff { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <inheritdoc />
    [Parameter] public ScreenReply Screen { get; set; } = null!;

    /// <summary>Opens a file's diff; if it can't be read, says why and leaves the list as it is.</summary>
    private async Task OpenAsync((GitFile File, bool Staged) change)
    {
        _error = null;
        try
        {
            _diff = await Api.GitDiffAsync(change.File.Path, change.Staged);
            _openKey = ChangesPanel.Key(change.File, change.Staged);
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

    /// <summary>Closes the diff, leaving the list.</summary>
    private void Close()
    {
        _diff = null;
        _openKey = null;
    }

    /// <summary>
    /// After staging, unstaging or discarding the open file, shows what's left of it: the same side if it still has
    /// changes there, otherwise the other side, otherwise it closes.
    /// </summary>
    private async Task ChangeOpenAsync(Func<IEnumerable<string>, Task> change)
    {
        var (path, staged) = (_diff!.Path, _diff.Staged);
        await change([path]);
        if (Git.Error is not null)
        {
            _error = Git.Error;
            return;
        }

        if (Git.Status?.Files.FirstOrDefault(file => file.Path == path) is not { } left)
        {
            Close();
            return;
        }

        await OpenAsync((left, (staged ? left.Staged : left.Unstaged) ? staged : !staged));
    }

    /// <summary>Opens the chat with a review of the open diff as a draft.</summary>
    private void ReviewOpen() =>
        Handoff.OpenChatWith(ReviewActions.Diff(_diff!.Path, _diff.Staged, _diff.Untracked).Prompt, Navigation);

    /// <summary>Opens the chat with a review of everything uncommitted as a draft.</summary>
    private void ReviewAll() => Handoff.OpenChatWith(ReviewActions.AllChanges().Prompt, Navigation);
}
