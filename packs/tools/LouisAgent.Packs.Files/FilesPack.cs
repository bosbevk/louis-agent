using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Packs.Files.Documents;
using LouisAgent.Packs.Files.Messages;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Files;

/// <summary>The workspace's files as tools, with PDF, Word and Excel read as text.</summary>
public sealed class FilesPack : IAgentPack
{
    /// <inheritdoc/>
    public string Name => FilesToolset.Files.Wire();

    /// <inheritdoc/>
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(FilesToolset.Files.Wire(), "Read, write, search, copy, rename and delete files in the workspace",
            GuideFile: PackToolset.Guide(FilesToolset.Files.Wire()),
            MissingSettings: _ => [],
            // Five classes, one concern each: a file's content, the tree's shape, browsing, searching, documents.
            ToolTypes:
            [
                typeof(WorkspaceFileTools), typeof(WorkspaceOrganiseTools), typeof(WorkspaceBrowseTools),
                typeof(WorkspaceSearchTools), typeof(DocumentTools)
            ],
            CreateTools: context =>
            {
                string root = Path.GetFullPath(context.WorkspaceRoot);
                return
                [
                    new WorkspaceFileTools(root), new WorkspaceOrganiseTools(root), new WorkspaceBrowseTools(root),
                    new WorkspaceSearchTools(root), new DocumentTools(root),
                ];
            }),
    ];

    /// <inheritdoc/>
    public bool ComesBeforeTheEngine => true;

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } = EmbeddedPrompts.Of(typeof(FilesPack).Assembly);

    /// <inheritdoc/>
    public IReadOnlyList<string> Requires => [];

    /// <inheritdoc/>
    public IReadOnlyList<IDataScreen> Screens { get; } =
    [
        new OwnViewScreen("workspace-files", MessageText.Format(FilesScreenMessage.Title),
            MessageText.Format(FilesScreenMessage.Description))
    ];
}

/// <summary>The name this pack's toolset goes by (ADR 0043).</summary>
public enum FilesToolset
{
    Files,
}
