using LouisAgent.Composition.Packs.Screens.Reading;

namespace LouisAgent.Packs.Files.Tests;

/// <summary>
/// The files pack offers its browser as an admin screen. It has no rows to describe, so its own view draws it, and a
/// site that doesn't carry that view doesn't list it.
/// </summary>
public class FilesPackScreensTests
{
    /// <summary>One screen, with a heading and no columns.</summary>
    [Test]
    public void ThePack_OffersItsBrowser()
    {
        var spec = new FilesPack().Screens.Single().Spec;

        Assert.That(spec.Name, Is.EqualTo("workspace-files"));
        Assert.That(spec.Title, Is.EqualTo("Files"));
        Assert.That(spec.Description, Is.Not.Empty);
        Assert.That(spec.Columns, Is.Empty);
    }

    /// <summary>There are no rows to read: everything comes from the workspace routes.</summary>
    [Test]
    public async Task TheScreen_HasNoRows() =>
        Assert.That(await new FilesPack().Screens.Single().ReadAsync(new ScreenQuery(null, 1), CancellationToken.None),
            Is.EqualTo(ScreenPage.Empty));
}
