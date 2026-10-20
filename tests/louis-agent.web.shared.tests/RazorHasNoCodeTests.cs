using System.Text.RegularExpressions;

namespace LouisAgent.Web.Shared.Tests;

/// <summary>
/// A .razor holds markup only (docs/CLAUDE.md, *Separate by concern*): a component's C# lives in its .razor.cs
/// code-behind, and logic that needs no component in a plain class with its own tests. A ratchet: the files that still
/// carry an @code block today are listed in <see cref="PendingCode"/>, a new one fails the test, and a listed file that
/// has lost its block fails too, so the list can only shrink until it is deleted and the test is a plain rule (W2b).
/// </summary>
[TestFixture]
public class RazorHasNoCodeTests
{
    private static readonly Regex CodeBlock = new(@"^\s*@code\b", RegexOptions.Multiline);

    /// <summary>Repo-relative, forward slashes; remove a file's line in the commit that moves its code out.</summary>
    private static readonly HashSet<string> PendingCode =
    [
        "src/louis-agent.puls/Components/Admin/ChartInfo.razor",
        "src/louis-agent.puls/Components/Admin/RunExplorer.razor",
        "src/louis-agent.puls/Components/Admin/TrendChart.razor",
        "src/louis-agent.puls/Components/Brand/PulsLogo.razor",
        "src/louis-agent.puls/Components/Brand/PulsTile.razor",
        "src/louis-agent.puls/Components/Brand/Water.razor",
        "src/louis-agent.puls/Components/Landing/Faq.razor",
        "src/louis-agent.puls/Components/Landing/Features.razor",
        "src/louis-agent.puls/Components/Landing/InsideTheApp.razor",
        "src/louis-agent.puls/Components/Landing/LandingDemo.razor",
        "src/louis-agent.puls/Components/Landing/MiniChat.razor",
        "src/louis-agent.puls/Components/Landing/Numbers.razor",
        "src/louis-agent.puls/Components/Landing/Pricing.razor",
        "src/louis-agent.puls/Pages/Admin/BenchmarkRun.razor",
        "src/louis-agent.puls/Pages/Admin/Benchmarks.razor",
        "src/louis-agent.puls/Pages/LandingPage.razor",
    ];

    [Test]
    public void ARazorFile_HoldsNoCodeBlock_UnlessStillPending()
    {
        string src = Path.Combine(TestPaths.RepoRoot, "src");
        var withCode = Directory.EnumerateFiles(src, "*.razor", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Where(f => CodeBlock.IsMatch(File.ReadAllText(f)))
            .Select(f => Path.GetRelativePath(TestPaths.RepoRoot, f).Replace('\\', '/'))
            .ToHashSet();

        var newOffenders = withCode.Except(PendingCode).Order().ToList();
        var done = PendingCode.Except(withCode).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(newOffenders, Is.Empty,
                "A .razor holds markup only: move the @code block to a .razor.cs code-behind:\n  " + string.Join("\n  ", newOffenders));
            Assert.That(done, Is.Empty,
                "No @code block any more: remove from PendingCode so the list only shrinks:\n  " + string.Join("\n  ", done));
        });
    }
}
