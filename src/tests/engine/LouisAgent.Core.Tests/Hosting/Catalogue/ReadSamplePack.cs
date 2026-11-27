using LouisAgent.Composition.Packs;
using LouisAgent.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Core.Tests.Hosting.Catalogue;

/// <summary>
/// A made-up pack that needs a model (F171.1): the program's catalogue builds it with the model it picked, and its
/// tool finds the chat's files through the pack context. It has no guide, so no rule of the test run's guides counts
/// it; its prompts are one empty provider for the whole run, registered before any prompt is read
/// (<see cref="TestPrompts" />), as a host registers a real pack's.
/// </summary>
public sealed class ReadSamplePack(IChatClient model) : IAgentPack
{
    /// <summary>The pack's name, and its one toolset's.</summary>
    public const string PackName = "readsample";

    /// <summary>Its prompts: none, held as one provider so it is registered once.</summary>
    public static readonly IFileProvider Guides = new FakePrompts(new Dictionary<string, string>());

    /// <inheritdoc/>
    public string Name => PackName;

    /// <inheritdoc/>
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        FakePack.Toolset(PackName, typeof(ReadSampleTools), context => [new ReadSampleTools(model, context.Files)])
            with { Skill = null }
    ];

    /// <inheritdoc/>
    public IFileProvider Prompts => Guides;

    /// <inheritdoc/>
    public IReadOnlyList<string> Requires => [];
}
