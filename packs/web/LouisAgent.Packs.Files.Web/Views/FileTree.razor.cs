using LouisAgent.Packs.Files.Web.Reading;
using LouisAgent.Web.Api;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Packs.Files.Web.Views;

/// <summary>One folder of the file browser. An opened folder shows another of these, one level deeper.</summary>
public partial class FileTree
{
    [Parameter] public string Path { get; set; } = "";
    [Parameter] public int Depth { get; set; }
    [Parameter] public string? SelectedPath { get; set; }
    [Parameter] public EventCallback<string> OnOpenFile { get; set; }

    private List<WorkspaceEntry>? _entries;
    private string? _error;
    private readonly HashSet<string> _open = [];

    /// <summary>Fetches what's in this folder; with no key, it says to enter one.</summary>
    protected override async Task OnInitializedAsync()
    {
        try
        {
            _entries = (await Api.ListFolderAsync(Path)).Entries;
        }
        catch (Exception ex)
        {
            _error = ex is AgentApiException { Status: System.Net.HttpStatusCode.Unauthorized }
                ? "Enter the API key in the chat's settings to browse files."
                : ex.Message;
        }
    }

    /// <summary>Opens a folder, or closes it if it was open.</summary>
    private void Toggle(WorkspaceEntry folder)
    {
        if (!_open.Remove(folder.Path)) _open.Add(folder.Path);
    }
}
