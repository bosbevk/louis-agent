namespace LouisAgent.Core.Tools.Workspace.Listing;

using System.Globalization;

/// <summary>A file's size as the listing tools say it (S, CL15, from WorkspaceTools).</summary>
internal static class FileSizeText
{
    /// <summary>Bytes, then KB, MB and GB to two decimals, never beyond GB.</summary>
    public static string Format(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        // Invariant, as every number the model reads is (Messages and logging): "1.5 KB" on any machine.
        return string.Create(CultureInfo.InvariantCulture, $"{len:0.##} {sizes[order]}");
    }
}
