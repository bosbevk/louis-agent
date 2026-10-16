namespace LouisAgent.Core.Skills;

using Microsoft.Extensions.FileProviders;

/// <summary>
/// Reads a built-in prompt (CL8 step 4, ADR 0017): the files in core's <c>Prompts/</c>, embedded with a manifest so they
/// keep their folder paths (<c>agent/puls.md</c>, <c>toolsets/web.md</c>), and read only from the assembly. A copy on
/// disk never overrides one, so a wording change is a rebuild. Files the agent writes stay on disk (<c>AgentHost</c>).
/// </summary>
public static class SkillFileSource
{
    // The csproj embeds Prompts/**; the provider serves them from that folder, by their paths under it.
    private const string Root = "Prompts";

    private static readonly ManifestEmbeddedFileProvider Provider = new(typeof(SkillFileSource).Assembly, Root);

    /// <summary>Every built-in prompt's path under <c>Prompts/</c>, with forward slashes, sorted.</summary>
    public static IReadOnlyList<string> EmbeddedFiles { get; } = AllFiles("").Order(StringComparer.Ordinal).ToList();

    /// <summary>The file names directly in <paramref name="folder"/> (not its subfolders); none when it isn't there.</summary>
    public static IReadOnlyList<string> Files(string folder) =>
        Provider.GetDirectoryContents(folder).Where(f => !f.IsDirectory).Select(f => f.Name).Order(StringComparer.Ordinal).ToList();

    /// <summary>The built-in prompt at <paramref name="path"/>, or null when there is none.</summary>
    public static string? ReadEmbedded(string path)
    {
        var file = Provider.GetFileInfo(path);
        if (!file.Exists || file.IsDirectory) return null;
        using var reader = new StreamReader(file.CreateReadStream());
        return reader.ReadToEnd();
    }

    private static IEnumerable<string> AllFiles(string folder) =>
        // The manifest's root is "/" ("" lists nothing).
        Provider.GetDirectoryContents(folder.Length == 0 ? "/" : folder).SelectMany(f =>
        {
            string path = folder.Length == 0 ? f.Name : $"{folder}/{f.Name}";
            return f.IsDirectory ? AllFiles(path) : [path];
        });
}
