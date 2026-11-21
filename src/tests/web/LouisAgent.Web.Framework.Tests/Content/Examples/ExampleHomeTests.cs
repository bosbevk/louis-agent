using LouisAgent.Web.Framework.Content.Examples;

namespace LouisAgent.Web.Framework.Tests.Content.Examples;

/// <summary>
/// F166.4: an example lives in a folder of this repository or at a path in a repository on GitHub, and says which,
/// its name and its link itself, so the page and its test ask it instead of working it out (Louis's pick A of
/// finding 3).
/// </summary>
public class ExampleHomeTests
{
    /// <summary>A home here is its folder in this repository, named by its last part.</summary>
    [Test]
    public void AHomeHere_IsAFolderOfThisRepository()
    {
        const string folder = "demos/louis-agent/identity/LouisAgent.Identities.Coding";
        var home = ExampleHome.Here(folder);

        Assert.Multiple(() =>
        {
            Assert.That(home.IsHere, Is.True);
            Assert.That(home.Name, Is.EqualTo("LouisAgent.Identities.Coding"));
            Assert.That(home.Link, Is.EqualTo("https://github.com/bosbevk/louis-agent/tree/main/" + folder));
        });
    }

    /// <summary>A home elsewhere is a path in another repository, linked there, with its repository's page.</summary>
    [Test]
    public void AHomeElsewhere_IsAPathInAnotherRepository()
    {
        var home = ExampleHome.Elsewhere("bosbevk/puls", "src/Puls.Identity");

        Assert.Multiple(() =>
        {
            Assert.That(home.IsHere, Is.False);
            Assert.That(home.Name, Is.EqualTo("Puls.Identity"));
            Assert.That(home.Link, Is.EqualTo("https://github.com/bosbevk/puls/tree/main/src/Puls.Identity"));
            Assert.That(home.Repository, Is.EqualTo("https://github.com/bosbevk/puls"));
        });
    }
}
