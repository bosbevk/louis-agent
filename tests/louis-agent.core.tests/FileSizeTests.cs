namespace LouisAgent.Core.Tests;

// docs/CLAUDE.md, "Separate by concern": a file holds at most 200 lines of code, a test file 400 (blank and comment
// lines don't count, ADR 0044); a bigger class is split by
// responsibility. A ratchet (S, CL15): the files over their limit today are listed in Oversized, a new one fails the
// test, and a listed file that has come under its limit fails too, so the list can only shrink until it is deleted and
// the test is a plain rule. Counted from the source files, bin/ and obj/ skipped.
public class FileSizeTests
{
    private const int SourceLimit = 200;
    private const int TestLimit = 400;

    private static readonly string[] SourceRoots =
    [
        "louis-agent.core", "louis-agent.choices", "louis-agent.session-stream", "identities", "packs", "src", "tools",
        "evals"
    ];

    /// <summary>Repo-relative, forward slashes, generated from the tree on 2026-10-20; remove a file's line in the commit that splits it.</summary>
    private static readonly HashSet<string> Oversized =
    [
        "louis-agent.core/Agent/EngineTools/AgentBuiltTools.cs",
        "louis-agent.core/Agent/EngineTools/SkillTools.cs",
        "louis-agent.core/Tools/DotNet/DotNetTools.cs",
        "louis-agent.core/Tools/Integrations/DevOps/DevOpsTools.cs",
        "louis-agent.core/Tools/Integrations/Paymo/PaymoTools.cs",
        "louis-agent.core/Tools/Shell/PythonTools.cs",
        "louis-agent.core/Tools/Web/Browsing/ChromeWebBrowser.cs",
        "src/louis-agent.api/Benchmarks/Store/BenchmarkRecorder.cs",
        "src/louis-agent.api/Benchmarks/Store/MongoBenchmarkStore.cs",
        "src/louis-agent.orchestrator/CommsLog.cs",
        "src/louis-agent.orchestrator/Program.cs",
        "src/louis-agent.orchestrator/Tools/ServiceTools.cs",
        "src/louis-agent.puls/Components/Admin/RunExplorer.razor",
        "src/louis-agent.puls/Pages/Admin/Benchmarks.razor",
        "src/louis-agent.puls/Pages/ChatPage.razor.cs",
        "src/louis-agent.web/Pages/ChatPage.razor.cs",
        "tests/louis-agent.core.tests/Tools/Integrations/DevOps/DevOpsToolsTests.cs",
        "tests/louis-agent.core.tests/Tools/Integrations/Paymo/PaymoToolsTests.cs",
        "tools/louis-agent.usage-probe/Program.cs",
    ];

    [Test]
    public void AFile_HoldsAtMost200Lines_ATestFile400_UnlessStillOversized()
    {
        var over = new HashSet<string>();
        foreach (var (root, limit) in SourceRoots.Select(r => (r, SourceLimit)).Append(("tests", TestLimit)))
        {
            string dir = Path.Combine(TestPaths.RepoRoot, root);
            if (!Directory.Exists(dir)) continue;
            foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                if (!(file.EndsWith(".cs") || file.EndsWith(".razor")) || InBuildOutput(file)) continue;
                if (CountCodeLines(File.ReadAllLines(file)) > limit)
                    over.Add(Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'));
            }
        }

        var newOffenders = over.Except(Oversized).Order().ToList();
        var shrunk = Oversized.Except(over).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(newOffenders, Is.Empty,
                $"A file holds at most {SourceLimit} lines (a test file {TestLimit}): split by responsibility (docs/CLAUDE.md, Separate by concern):\n  "
                + string.Join("\n  ", newOffenders));
            Assert.That(shrunk, Is.Empty,
                "Within its limit now: remove from Oversized so the list only shrinks:\n  " +
                string.Join("\n  ", shrunk));
        });
    }

    /// <summary>How long a file is for the size rule: every line that isn't blank or a comment.</summary>
    internal static int CountCodeLines(IEnumerable<string> lines) =>
        lines.Select(line => line.TrimStart()).Count(line => line.Length > 0 && !line.StartsWith("//"));

    private static bool InBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}");
}
