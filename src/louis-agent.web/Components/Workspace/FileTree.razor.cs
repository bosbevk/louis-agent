using LouisAgent.Web.Shared.Api;
using LouisAgent.Web.Workspace;
using Microsoft.AspNetCore.Components;

namespace LouisAgent.Web.Components.Workspace;

public partial class FileTree
{
    [Parameter] public string Path { get; set; } = "";
    [Parameter] public int Depth { get; set; }
    [Parameter] public string? SelectedPath { get; set; }
    [Parameter] public EventCallback<string> OnOpenFile { get; set; }

    private List<WorkspaceEntry>? _entries;
    private string? _error;
    private readonly HashSet<string> _open = [];

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _entries = (await Api.ListFolderAsync(Path)).Entries;
        }
        catch (Exception ex)
        {
            _error = ex is AgentApiException { Status: System.Net.HttpStatusCode.Unauthorized }
                ? "Enter the API key below to browse files."
                : ex.Message;
        }
    }

    private void Toggle(WorkspaceEntry folder)
    {
        if (!_open.Remove(folder.Path)) _open.Add(folder.Path);
    }
}
