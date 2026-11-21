namespace LouisAgent.Packs.Rules.Tests;

/// <summary>
/// The rules hold every pack only if the list is every pack: one entry for each project in <c>packs/tools</c>.
/// </summary>
public class EveryPackTests
{
    /// <summary>The list has as many packs as <c>packs/tools</c> has projects, and no two by one name.</summary>
    [Test]
    public void TheList_IsEveryPackInTheToolsFolder()
    {
        int projects = Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, "packs/tools"), "*.csproj",
            SearchOption.AllDirectories).Count();

        Assert.Multiple(() =>
        {
            Assert.That(EveryPack.All, Has.Count.EqualTo(projects));
            Assert.That(EveryPack.All.Select(pack => pack.Name), Is.Unique);
        });
    }
}
