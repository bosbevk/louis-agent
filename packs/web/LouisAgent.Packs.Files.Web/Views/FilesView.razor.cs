using LouisAgent.Api.Contracts.Admin.Screens;
using LouisAgent.Packs.Files.Web.Reading;
using LouisAgent.Web.Api;
using LouisAgent.Web.Components.Chat.Shell;
using LouisAgent.Web.Components.Packs;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Files.Web.Views;

/// <summary>
/// The workspace's files as an admin screen: the tree beside the open file. Its buttons hand the chat a draft and never
/// send anything, so the person reads the prompt first.
/// </summary>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0086-a-packs-screens-are-described.md">ADR 0086: a pack's screens are described</seealso>
public partial class FilesView : IScreenView
{
    private WorkspaceFile? _file;
    private string? _error;

    [Inject] private AgentApi Api { get; set; } = null!;
    [Inject] private ChatHandoff Handoff { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    /// <inheritdoc />
    [Parameter] public ScreenReply Screen { get; set; } = null!;

    /// <summary>Opens a file; if it can't be read, says why and leaves the tree as it is.</summary>
    private async Task OpenAsync(string path)
    {
        _error = null;
        try
        {
            _file = await Api.ReadFileAsync(path);
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

    /// <summary>Opens the chat with a review of this file as a draft.</summary>
    private void Review() => Handoff.OpenChatWith(FilePrompts.Review(_file!.Path), Navigation);

    /// <summary>Opens the chat with the file's name ready, for the person to finish the question.</summary>
    private void Ask() => Handoff.OpenChatWith(FilePrompts.Ask(_file!.Path), Navigation);

    /// <summary>Closes the file, leaving the tree.</summary>
    private void Close() => _file = null;
}
