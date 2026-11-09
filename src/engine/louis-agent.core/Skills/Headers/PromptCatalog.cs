namespace LouisAgent.Core.Skills.Headers;

/// <summary>
/// Every built-in prompt (core's, and the identities' and packs' a host registered), read once with its header
/// (CL27): the loader asks it for the body the model reads, a session for the toolsets a role always loads, the portal
/// for the list. A folder's README is no prompt.
/// </summary>
/// <remarks>
/// Lazy Load (Fowler, PoEAA) over <see cref="SkillFileSource"/>: read on the first ask, after the host has registered
/// every identity's and pack's prompts (a registration after the first read already fails loudly there), and never
/// again. Every header is read at once, so one faulty header stops the host when it starts, not when a chat opens
/// that role.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0018-built-in-prompts-embedded.md">ADR 0018: the built-in prompts are embedded</seealso>
public static class PromptCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, PromptFile>> Files = new(Read);

    /// <summary>Every built-in prompt, by its path under <c>prompts/</c>.</summary>
    public static IReadOnlyList<PromptFile> All => [.. Files.Value.Values.OrderBy(p => p.Path, StringComparer.Ordinal)];

    /// <summary>The prompt at this path, or none when there is no built-in prompt there.</summary>
    public static PromptFile? Get(string path) => Files.Value.GetValueOrDefault(path);

    /// <summary>Reads every markdown file the source embeds, each header with it.</summary>
    private static IReadOnlyDictionary<string, PromptFile> Read() =>
        SkillFileSource.EmbeddedFiles
            .Where(path => path.EndsWith(".md", StringComparison.Ordinal) &&
                           Path.GetFileName(path) != SkillFiles.FolderMarker)
            .ToDictionary(path => path, path => PromptHeaders.Split(path, SkillFileSource.ReadEmbedded(path)!),
                StringComparer.Ordinal);
}
