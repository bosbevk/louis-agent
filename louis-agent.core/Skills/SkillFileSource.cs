namespace LouisAgent.Core.Skills;

using LouisAgent.Core.Messages;
using Microsoft.Extensions.FileProviders;

/// <summary>
/// Reads a built-in prompt (CL8 step 4, ADR 0018): the files in core's <c>Prompts/</c>, embedded with a manifest so they
/// keep their folder paths (<c>toolsets/web.md</c>), and from CL15 the identities' and packs' prompts the host registers
/// with <see cref="Use"/> before the first read, all read only from their assemblies. A copy on disk never overrides
/// one, so a wording change is a rebuild. Files the agent writes stay on disk (<c>AgentHost</c>). Static, so there is
/// one reader (ADR 0018), and lazy, so the host can register prompts first: a new provider after the first read fails
/// loudly, and one already registered is skipped, so a host or test that builds twice is fine.
/// </summary>
public static class SkillFileSource
{
    // The csproj embeds Prompts/**; the provider serves them from that folder, by their paths under it.
    private const string Root = "Prompts";

    private static readonly ManifestEmbeddedFileProvider Core = new(typeof(SkillFileSource).Assembly, Root);
    private static readonly List<IFileProvider> Extra = [];
    private static readonly Lock Gate = new();
    private static CompositeFileProvider? _provider;
    private static IReadOnlyList<string>? _embeddedFiles;

    /// <summary>Registers identities' and packs' prompts (CL15): before the first read, and each provider once.</summary>
    public static void Use(IEnumerable<IFileProvider> prompts)
    {
        lock (Gate)
        {
            var fresh = prompts.Where(p => !ReferenceEquals(p, Core) && !Extra.Contains(p)).Distinct().ToList();
            if (fresh.Count == 0) return;
            if (_provider is not null)
                throw new InvalidOperationException(MessageText.Format(ErrorMessage.PromptsAddedAfterRead));
            Extra.AddRange(fresh);
        }
    }

    /// <summary>Every provider the reader reads: core's, then the ones <see cref="Use"/> registered.</summary>
    public static IReadOnlyList<IFileProvider> Providers
    {
        get { lock (Gate) return [Core, .. Extra]; }
    }

    private static IFileProvider Provider
    {
        get { lock (Gate) return _provider ??= new CompositeFileProvider(Providers); }
    }

    /// <summary>Every built-in prompt's path under <c>Prompts/</c>, with forward slashes, sorted: core's and the registered ones'.</summary>
    public static IReadOnlyList<string> EmbeddedFiles
    {
        get { lock (Gate) return _embeddedFiles ??= FilesOf(Provider).Order(StringComparer.Ordinal).ToList(); }
    }

    /// <summary>Core's own prompts' paths, whatever was registered: what the catalogue checks a pack's paths against.</summary>
    public static IReadOnlyList<string> CoreFiles { get; } = FilesOf(Core).Order(StringComparer.Ordinal).ToList();

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

    /// <summary>Every file a provider holds, at any depth, with forward slashes.</summary>
    internal static IEnumerable<string> FilesOf(IFileProvider provider) => AllFiles(provider, "");

    private static IEnumerable<string> AllFiles(IFileProvider provider, string folder) =>
        // The manifest's root is "/" ("" lists nothing).
        provider.GetDirectoryContents(folder.Length == 0 ? "/" : folder).SelectMany(f =>
        {
            string path = folder.Length == 0 ? f.Name : $"{folder}/{f.Name}";
            return f.IsDirectory ? AllFiles(provider, path) : [path];
        });
}
