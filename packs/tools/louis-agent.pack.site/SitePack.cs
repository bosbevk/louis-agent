using LouisAgent.Choices;
using LouisAgent.Composition.Packs;
using LouisAgent.Packs.Site.Tools;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Site;

/// <summary>
/// The guide's way to take a visitor to a page of the site, and to an area of it (F117): one toolset, <c>site</c>,
/// with one tool that names the page. The web's widget hears the call and moves the browser.
/// </summary>
/// <remarks>
/// The same shape as every pack (ADR 0027): a list of toolsets and the files the model reads, naming no identity and
/// no other pack, and needing no setting. A tool and not a prompt rule (ADR 0080): the guide takes the visitor there
/// instead of telling them where to click.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0027-engine-identities-and-packs.md">ADR 0027: engine, identities and packs</seealso>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0080-a-tool-when-it-improves-the-prompt.md">ADR 0080: a tool when it improves the prompt</seealso>
public sealed class SitePack : IAgentPack
{
    /// <inheritdoc />
    public string Name => SiteToolset.Site.Wire();

    /// <inheritdoc />
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(SiteToolset.Site.Wire(),
            "Take the visitor to a page of this site, or to an area of it",
            GuideFile: PackToolset.Guide(SiteToolset.Site.Wire()),
            MissingSettings: _ => [],
            ToolTypes: [typeof(SiteTools)],
            CreateTools: _ => [new SiteTools()]),
    ];

    /// <inheritdoc />
    public IFileProvider Prompts { get; } = EmbeddedPrompts.Of(typeof(SitePack).Assembly);

    /// <inheritdoc />
    public IReadOnlyList<string> Requires => [];

    /// <inheritdoc />
    public bool ComesBeforeTheEngine => true;
}
