namespace LouisAgent.HostPacks.Tests.Copies;

/// <summary>
/// The recipes pack keeps its own copy of the two page readers, since a pack can't reference the engine or another pack.
/// The copies stay the same as the originals, apart from their namespace, so a fix to one can't quietly miss the other.
/// </summary>
public class PageReaderCopyTests
{
    /// <summary>A reader in the recipes pack and its original say the same thing.</summary>
    [TestCase("packs/louis-agent.pack.recipes/Reading/HtmlText.cs",
        "src/engine/louis-agent.core/Tools/Web/Fetching/HtmlText.cs")]
    [TestCase("packs/louis-agent.pack.recipes/Reading/LeadImage.cs",
        "src/engine/louis-agent.core/Tools/Web/Fetching/LeadImage.cs")]
    [TestCase("packs/louis-agent.pack.recipes/Reading/JsonLdBlocks.cs",
        "src/engine/louis-agent.core/Tools/Web/StructuredData/JsonLdBlocks.cs")]
    public void TheCopy_IsTheOriginal(string copy, string original) =>
        Assert.That(Code(copy), Is.EqualTo(Code(original)));

    /// <summary>A file's lines without its namespace line, and without what only says where it lives.</summary>
    private static IEnumerable<string> Code(string path) =>
        File.ReadAllLines(Path.Combine(TestPaths.RepoRoot, path))
            .Where(line => !line.StartsWith("namespace ", StringComparison.Ordinal)
                           && !line.StartsWith("using LouisAgent.", StringComparison.Ordinal));
}
