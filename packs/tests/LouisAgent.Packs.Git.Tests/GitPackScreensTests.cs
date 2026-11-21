using LouisAgent.Composition.Packs.Screens.Reading;

namespace LouisAgent.Packs.Git.Tests;

/// <summary>
/// The git pack offers its controls as admin screens. They have no rows to describe, so their own views draw them, and
/// a site that doesn't carry those views doesn't list them.
/// </summary>
public class GitPackScreensTests
{
    /// <summary>Changes and Branches, each with a heading and no columns.</summary>
    [Test]
    public void ThePack_OffersChangesAndBranches()
    {
        var specs = new GitPack().Screens.Select(screen => screen.Spec).ToList();

        Assert.That(specs.Select(spec => spec.Name), Is.EqualTo(new[] { "git-changes", "git-branches" }));
        Assert.That(specs.Select(spec => spec.Title), Is.EqualTo(new[] { "Changes", "Branches" }));
        Assert.That(specs.Select(spec => spec.Description), Has.All.Not.Empty);
        Assert.That(specs.SelectMany(spec => spec.Columns), Is.Empty);
    }

    /// <summary>There are no rows to read: everything comes from the git routes.</summary>
    [Test]
    public async Task AScreen_HasNoRows()
    {
        foreach (var screen in new GitPack().Screens)
            Assert.That(await screen.ReadAsync(new ScreenQuery(null, 1), CancellationToken.None),
                Is.EqualTo(ScreenPage.Empty));
    }
}
