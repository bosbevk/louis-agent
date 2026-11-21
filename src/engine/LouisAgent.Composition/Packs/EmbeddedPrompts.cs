using System.Reflection;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Composition.Packs;

/// <summary>
/// An identity's or a pack's prompts, as its project embeds them (F129): the folder they sit in is named here, once,
/// and every identity, pack and the engine read them through this.
/// </summary>
/// <remarks>
/// A Factory Method over the framework's embedded provider (Gang of Four): the folder's name was typed out in 21
/// places, so renaming it took 21 edits (Louis: "if there was an enum we would not have had this problem"). One value,
/// not a set to choose from, so a constant beside the factory rather than an enum; <c>PromptFolderTests</c> fails on
/// any other source that names the folder or builds the provider.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0043-repeated-names-are-enums.md">ADR 0043: repeated names are enums</seealso>
public static class EmbeddedPrompts
{
    /// <summary>The folder a project's prompts sit in, as the project embeds it.</summary>
    public const string Folder = "prompts";

    /// <summary>Every prompt the assembly embeds, by its path under the folder.</summary>
    public static IFileProvider Of(Assembly assembly) => new ManifestEmbeddedFileProvider(assembly, Folder);

    /// <summary>
    /// The prompts under one subfolder, for a project that holds several identities' (the showroom's).
    /// </summary>
    public static IFileProvider Of(Assembly assembly, string subfolder) =>
        new ManifestEmbeddedFileProvider(assembly, $"{Folder}/{subfolder}");
}
