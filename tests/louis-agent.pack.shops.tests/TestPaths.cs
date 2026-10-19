namespace LouisAgent.Packs.Shops.Tests;

/// <summary>The repository root, for the fixtures beside these tests (they moved from core's tests with the pack, CL15).</summary>
internal static class TestPaths
{
    public static string RepoRoot { get; } =
        Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
}
