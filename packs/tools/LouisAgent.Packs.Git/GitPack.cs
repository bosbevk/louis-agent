using LouisAgent.Choices;
using LouisAgent.Composition.Messages;
using LouisAgent.Composition.Packs;
using LouisAgent.Composition.Packs.Screens;
using LouisAgent.Packs.Git.Messages;
using LouisAgent.Packs.Git.Running;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Packs.Git;

/// <summary>Git as tools, split by what each class does to the repository.</summary>
public sealed class GitPack : IAgentPack
{
    /// <inheritdoc/>
    public string Name => GitToolset.Git.Wire();

    /// <inheritdoc/>
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        new(GitToolset.Git.Wire(),
            "Status, diffs, branches, staging, commits, merges and history for the workspace repository",
            GuideFile: PackToolset.Guide(GitToolset.Git.Wire()),
            MissingSettings: _ => [],
            ToolTypes:
            [
                typeof(GitChangeTools), typeof(GitBranchTools), typeof(GitRemoteTools), typeof(GitHistoryTools),
                typeof(GitConfigTools)
            ],
            CreateTools: context =>
            {
                // One runner over the workspace, so all five classes work on the same repository.
                var git = new GitRunner(Path.GetFullPath(context.WorkspaceRoot));
                return
                [
                    new GitChangeTools(git), new GitBranchTools(git), new GitRemoteTools(git), new GitHistoryTools(git),
                    new GitConfigTools(git)
                ];
            }),
    ];

    /// <inheritdoc/>
    public bool ComesBeforeTheEngine => true;

    /// <inheritdoc/>
    public IFileProvider Prompts { get; } = EmbeddedPrompts.Of(typeof(GitPack).Assembly);

    /// <inheritdoc/>
    public IReadOnlyList<string> Requires => [];

    /// <inheritdoc/>
    public IReadOnlyList<IDataScreen> Screens { get; } =
    [
        new OwnViewScreen("git-changes", MessageText.Format(GitScreenMessage.ChangesTitle),
            MessageText.Format(GitScreenMessage.ChangesDescription)),
        new OwnViewScreen("git-branches", MessageText.Format(GitScreenMessage.BranchesTitle),
            MessageText.Format(GitScreenMessage.BranchesDescription))
    ];
}

/// <summary>The name this pack's toolset goes by (ADR 0043).</summary>
public enum GitToolset
{
    Git,
}
