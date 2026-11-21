using System.Text.RegularExpressions;

namespace LouisAgent.Core.Tests;

/// <summary>
/// The prompts' folder is named in one place (F129, Louis: "if there was an enum we would not have had this problem"):
/// renaming <c>Prompts</c> to <c>prompts</c> took 21 edits of the same string. Every identity, pack and the engine
/// read their prompts through <c>EmbeddedPrompts</c>, and no other source names the folder or builds the provider.
/// </summary>
public partial class PromptFolderTests
{
    /// <summary>
    /// The folder named as one: its provider built, a disk path to it, or a constant holding its name; a JSON or BSON
    /// key called <c>prompts</c> is another thing. Matches <c>Path.Combine(core, "prompts")</c>, not
    /// <c>GetProperty("prompts")</c>.
    /// </summary>
    [GeneratedRegex("""ManifestEmbeddedFileProvider\(|ManifestEmbeddedFileProvider \w+ = new\(""" +
                    """|Path\.Combine\([^;]*"[Pp]rompts"|= "[Pp]rompts";""", RegexOptions.NonBacktracking)]
    private static partial Regex FolderName();

    /// <summary>The source files a repo's projects build, tests included, the factory itself left out.</summary>
    private static IEnumerable<string> Sources() =>
        new[] { "src", "demos", "packs" }
            .SelectMany(root => Directory.EnumerateFiles(Path.Combine(TestPaths.RepoRoot, root), "*.cs",
                SearchOption.AllDirectories))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
                           && !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                           && Path.GetFileName(file) != "EmbeddedPrompts.cs"
                           && Path.GetFileName(file) != nameof(PromptFolderTests) + ".cs");

    /// <summary>No source but the factory names the folder or builds its provider.</summary>
    [Test]
    public void OnlyTheFactory_NamesThePromptsFolder()
    {
        var offenders = Sources()
            .Where(file =>
            {
                return FolderName().IsMatch(File.ReadAllText(file));
            })
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'))
            .ToList();

        Assert.That(offenders, Is.Empty, "read prompts through EmbeddedPrompts.Of, its folder named there alone");
    }
}
