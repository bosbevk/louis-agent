using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using LouisAgent.Packs.Docs.Tools;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Docs;

/// <summary>
/// A project's own docs as a pack (F109.3): one toolset, <c>docs</c>, that lists, reads and searches the markdown under
/// one folder and never writes or looks outside it, so a guide answers from what was written instead of from memory.
/// Without <c>DOCS_ROOT</c> it stays off.
/// </summary>
/// <remarks>
/// The same shape as every pack (ADR 0027): a list of toolsets and the files the model reads, naming no identity and no
/// other pack. General tools come before the engine's, as the files pack's do. A tool and not a digest in the prompt
/// (ADR 0080): the docs are read when a question needs them, and the answer can name the doc it read.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
public sealed class DocsPack(DocsOptions options) : IAgentPack
{
    /// <inheritdoc />
    public string Name => DocsToolset.Docs.Wire();

    /// <inheritdoc />
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(DocsToolset.Docs.Wire(),
            "List, read and search a project's own docs, read-only",
            GuideFile: PackToolset.Guide(DocsToolset.Docs.Wire()),
            MissingSettings: _ => options.Root is null ? [DocsOptions.RootSetting] : [],
            ToolTypes: [typeof(DocsTools)],
            CreateTools: _ => [new DocsTools(options.Root!)],
            // Reading changes nothing, so the model's calls may run side by side.
            ParallelSafe: true),
    ];

    /// <inheritdoc />
    public IFileProvider Prompts { get; } = new ManifestEmbeddedFileProvider(typeof(DocsPack).Assembly, "Prompts");

    /// <inheritdoc />
    public IReadOnlyList<string> Requires => [];

    /// <inheritdoc />
    public bool ComesBeforeTheEngine => true;
}
