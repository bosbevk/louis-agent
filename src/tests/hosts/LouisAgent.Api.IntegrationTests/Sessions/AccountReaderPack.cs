using LouisAgent.Composition.Packs;
using LouisAgent.Testing;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.FileProviders;

namespace LouisAgent.Api.IntegrationTests.Sessions;

/// <summary>
/// A made-up pack that needs a model (F171.1): the program's catalogue builds it with the model it picked, and its
/// tool reads the chat's photo or PDF. Its guide is one provider for the whole run, registered before any prompt is
/// read (<see cref="ApiIntegrationPrompts" />), as a host registers a real pack's.
/// </summary>
public sealed class AccountReaderPack(IChatClient model) : IAgentPack
{
    /// <summary>The pack's name, and its one toolset's.</summary>
    public const string PackName = "accountreader";

    /// <summary>Its one guide, held in memory: read a file the user sent with the tool, by its name.</summary>
    public static readonly IFileProvider Guides = new FakePrompts(new Dictionary<string, string>
    {
        [PackToolset.SkillFile(PackName)] =
            "# accountreader\n\nWhen the user sends a photo or a PDF of an account, read it with `ReadAccount`, " +
            "giving it the file's name, and answer with the account number and the total it found.\n"
    });

    /// <inheritdoc/>
    public string Name => PackName;

    /// <inheritdoc/>
    public IReadOnlyList<PackToolset> Toolsets { get; } =
    [
        FakePack.Toolset(PackName, typeof(AccountReaderTools),
            context => [new AccountReaderTools(model, context.Files)])
    ];

    /// <inheritdoc/>
    public IFileProvider Prompts => Guides;

    /// <inheritdoc/>
    public IReadOnlyList<string> Requires => [];
}
