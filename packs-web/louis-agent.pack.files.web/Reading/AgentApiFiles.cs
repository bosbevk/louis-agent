using System.Net.Http.Json;
using LouisAgent.Web.Api;

namespace LouisAgent.Packs.Files.Web.Reading;

/// <summary>The calls the file browser makes: what's in a folder, and one file to read.</summary>
public static class AgentApiFiles
{
    /// <summary>What's in one folder of the workspace.</summary>
    public static async Task<WorkspaceFolder> ListFolderAsync(this AgentApi api, string path)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"v1/workspace/entries?path={Uri.EscapeDataString(path)}"));
        return (await response.Content.ReadFromJsonAsync<WorkspaceFolder>())!;
    }

    /// <summary>One file from the workspace, to read.</summary>
    public static async Task<WorkspaceFile> ReadFileAsync(this AgentApi api, string path)
    {
        using var response = await api.SendAsync(new HttpRequestMessage(HttpMethod.Get,
            $"v1/workspace/file?path={Uri.EscapeDataString(path)}"));
        return (await response.Content.ReadFromJsonAsync<WorkspaceFile>())!;
    }
}
