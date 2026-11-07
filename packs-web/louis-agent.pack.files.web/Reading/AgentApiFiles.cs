using LouisAgent.Web.Api;

namespace LouisAgent.Packs.Files.Web.Reading;

/// <summary>The calls the file browser makes: what's in a folder, and one file to read.</summary>
public static class AgentApiFiles
{
    /// <summary>What's in one folder of the workspace.</summary>
    public static Task<WorkspaceFolder> ListFolderAsync(this AgentApi api, string path) =>
        api.GetAsync<WorkspaceFolder>($"v1/workspace/entries?path={Uri.EscapeDataString(path)}");

    /// <summary>One file from the workspace, to read.</summary>
    public static Task<WorkspaceFile> ReadFileAsync(this AgentApi api, string path) =>
        api.GetAsync<WorkspaceFile>($"v1/workspace/file?path={Uri.EscapeDataString(path)}");
}
