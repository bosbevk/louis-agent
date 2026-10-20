namespace LouisAgent.Web.Workspace;

/// <summary>A file's size as the viewer's header says it (W2b, from FileViewer).</summary>
public static class FileSize
{
    /// <summary>Bytes under a kilobyte, then KB and MB to one decimal.</summary>
    public static string Format(long bytes) =>
        bytes < 1024 ? $"{bytes} B" : bytes < 1024 * 1024 ? $"{bytes / 1024.0:0.#} KB" : $"{bytes / 1024.0 / 1024.0:0.#} MB";
}
