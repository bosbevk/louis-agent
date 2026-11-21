using LouisAgent.Evals.Readers.Kinds;

namespace LouisAgent.Packs.Recipes.Tests.Checks;

/// <summary>
/// A link reader that gives the link it's told, whatever the answer. The recipe rule's tests use it, since how an
/// answer writes its recipe link is an identity's reader's to know.
/// </summary>
/// <remarks>A Fake (Meszaros): the real reader is tested beside the identity that ships it.</remarks>
internal sealed class FakeLinks(string? link) : ILinkReader
{
    /// <summary>A reader that finds no link.</summary>
    public static FakeLinks None { get; } = new(null);

    /// <inheritdoc />
    public string Name => "fake-links";

    /// <inheritdoc />
    public string? RecipeLink(string answer) => link;
}
