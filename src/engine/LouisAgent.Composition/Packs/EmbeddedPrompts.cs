using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Composition.Packs;

/// <summary>
/// An identity's or a pack's prompts, as its project embeds them: the prompt folders at the project's root
/// (<c>agents/</c>, <c>toolsets/</c>, <c>shared/</c>), which its project file names, so a path in the project is the
/// path the engine reads (CL68.1). Every identity, pack and the engine read them through this.
/// </summary>
/// <remarks>
/// A Factory Method over the framework's embedded provider (Gang of Four): one place builds it, and
/// <c>PromptFolderTests</c> fails on any other source that builds the provider or wraps a project's prompts in a
/// folder. No filter around it (Louis's pick A of three for CL68.1): a project embeds nothing but its prompts.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class EmbeddedPrompts
{
    /// <summary>Every prompt the assembly embeds, by its path in the project.</summary>
    public static IFileProvider Of(Assembly assembly) => new ManifestEmbeddedFileProvider(assembly);

    /// <summary>
    /// The prompts under one folder of the project, for a project that holds several identities' (the showroom's).
    /// </summary>
    public static IFileProvider Of(Assembly assembly, string subfolder) =>
        new ManifestEmbeddedFileProvider(assembly, subfolder);
}
