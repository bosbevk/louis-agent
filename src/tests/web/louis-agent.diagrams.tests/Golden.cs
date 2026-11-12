using System.Runtime.CompilerServices;

namespace LouisAgent.Diagrams.Tests;

/// <summary>
/// Compares a drawing with its golden file beside the test, as the web's snapshots do: a difference fails with where
/// to read what was drawn, and <c>LOUIS_UPDATE_GOLDEN=1</c> writes the drawing as the new golden to review.
/// </summary>
internal static class Golden
{
    /// <summary>Fails unless <paramref name="actual"/> is the golden file's text, its line endings aside.</summary>
    public static void Match(string name, string actual, [CallerFilePath] string caller = "")
    {
        string path = Path.Combine(Path.GetDirectoryName(caller)!, name);
        if (Environment.GetEnvironmentVariable("LOUIS_UPDATE_GOLDEN") == "1")
            File.WriteAllText(path, actual);

        string drawn = Path.Combine(Path.GetTempPath(), "louis-agent-diagrams", name);
        Directory.CreateDirectory(Path.GetDirectoryName(drawn)!);
        File.WriteAllText(drawn, actual);
        Assert.That(File.Exists(path), Is.True, $"no golden {name}; what was drawn is in {drawn}");
        Assert.That(actual.ReplaceLineEndings(), Is.EqualTo(File.ReadAllText(path).ReplaceLineEndings()),
            $"{name} differs; what was drawn is in {drawn}");
    }
}
