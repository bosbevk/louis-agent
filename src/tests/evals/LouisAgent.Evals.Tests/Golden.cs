namespace LouisAgent.Evals.Tests;

/// <summary>
/// Compares a text with its golden file, as the diagrams' tests do: a difference fails with where to read what was
/// made, and <c>LOUIS_UPDATE_GOLDEN=1</c> writes the text as the new golden to review.
/// </summary>
internal static class Golden
{
    /// <summary>
    /// Fails unless <paramref name="actual"/> is the text of the golden file at <paramref name="path"/>, relative to
    /// the repository's root, its line endings aside.
    /// </summary>
    public static void Match(string path, string actual)
    {
        string golden = Path.Combine(TestPaths.RepoRoot, path);
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
            File.WriteAllText(golden, actual);

        string made = Path.Combine(Path.GetTempPath(), "louis-agent-evals", Path.GetFileName(path));
        Directory.CreateDirectory(Path.GetDirectoryName(made)!);
        File.WriteAllText(made, actual);
        Assert.That(File.Exists(golden), Is.True, $"no golden {path}; what was made is in {made}");
        Assert.That(actual.ReplaceLineEndings(), Is.EqualTo(File.ReadAllText(golden).ReplaceLineEndings()),
            $"{path} differs; what was made is in {made}");
    }
}
