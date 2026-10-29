namespace LouisAgent.Core.Tests;

/// <summary>
/// Keeps files small enough to read: at most 200 lines of code, a test file 400 (blank and comment lines don't count).
/// A bigger class is split by what it does (docs/CLAUDE.md, *Separate by concern*).
/// </summary>
/// <remarks>
/// A ratchet (S, CL15): the files over their limit today are listed in Oversized, a new one fails the test, and a
/// listed file that has come under its limit fails too, so the list can only shrink until it is deleted and the test
/// is a plain rule. Counted from the source files, bin/ and obj/ skipped.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/docs/adr/0044-the-size-limit-counts-code.md">ADR 0044: the size limit counts code</seealso>
public class FileSizeTests
{
    private const int SourceLimit = 200;
    private const int TestLimit = 400;

    private static readonly string[] SourceRoots =
    [
        "louis-agent.core", "louis-agent.choices", "louis-agent.session-stream", "identities", "packs", "src", "tools",
        "evals"
    ];

    /// <summary>
    /// Repo-relative, forward slashes, generated from the tree on 2026-10-20; remove a file's line in the commit that
    /// splits it.
    /// </summary>
    private static readonly HashSet<string> Oversized =
    [
        "louis-agent.core/Agent/EngineTools/AgentBuiltTools.cs",
        "louis-agent.core/Agent/EngineTools/SkillTools.cs",
        "louis-agent.core/Tools/DotNet/DotNetTools.cs",
        "louis-agent.core/Tools/Integrations/DevOps/DevOpsTools.cs",
        "louis-agent.core/Tools/Integrations/Paymo/PaymoTools.cs",
        "louis-agent.core/Tools/Shell/PythonTools.cs",
        "louis-agent.core/Tools/Web/Browsing/ChromeWebBrowser.cs",
        "src/louis-agent.orchestrator/CommsLog.cs",
        "src/louis-agent.orchestrator/Program.cs",
        "src/louis-agent.orchestrator/Tools/ServiceTools.cs",
        "src/louis-agent.puls/Pages/ChatPage.razor.cs",
        "src/louis-agent.web/Pages/ChatPage.razor.cs",
        "tests/louis-agent.core.tests/Tools/Integrations/DevOps/DevOpsToolsTests.cs",
        "tests/louis-agent.core.tests/Tools/Integrations/Paymo/PaymoToolsTests.cs",
        "tools/louis-agent.usage-probe/Program.cs",
    ];

    /// <summary>
    /// No file is over its limit unless it is still listed, and a listed file that has come under it is taken off.
    /// </summary>
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
                // Only what we wrote and know how to size: C#, markup and stylesheets.
                if (InBuildOutput(file) || Lines(file) is not { } lines) continue;
                if (lines > limit) over.Add(Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'));
            }
        }

        var newOffenders = over.Except(Oversized).Order().ToList();
        var shrunk = Oversized.Except(over).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(newOffenders, Is.Empty,
                $"A file holds at most {SourceLimit} lines (a test file {TestLimit}): split by responsibility "
                + "(docs/CLAUDE.md, Separate by concern):\n  "
                + string.Join("\n  ", newOffenders));
            Assert.That(shrunk, Is.Empty,
                "Within its limit now: remove from Oversized so the list only shrinks:\n  " +
                string.Join("\n  ", shrunk));
        });
    }

    /// <summary>
    /// A file's size for the rule: C# and markup by their code lines, a stylesheet by the lines with a rule on them
    /// (CL26); any other file isn't sized.
    /// </summary>
    /// <remarks>A switch on the file's type, one arm per kind, instead of a chain of ifs.</remarks>
    private static int? Lines(string file) => Path.GetExtension(file) switch
    {
        ".cs" or ".razor" => CountCodeLines(File.ReadAllLines(file)),
        ".css" => CountCssLines(File.ReadAllLines(file)),
        _ => null
    };

    /// <summary>How long a file is for the size rule: every line that isn't blank or a comment.</summary>
    internal static int CountCodeLines(IEnumerable<string> lines) =>
        lines.Select(line => line.TrimStart()).Count(line => line.Length > 0 && !line.StartsWith("//"));

    /// <summary>
    /// How long a stylesheet is for the size rule: every line with a rule on it, comments and blanks left out.
    /// </summary>
    internal static int CountCssLines(IEnumerable<string> lines)
    {
        int count = 0;
        bool inComment = false;
        foreach (string line in lines)
        {
            // Take the comments out of the line, one opening or closing mark at a time; a comment can start on an
            // earlier line and end on this one.
            var rest = new System.Text.StringBuilder();
            int at = 0;
            while (at < line.Length)
            {
                string mark = inComment ? "*/" : "/*";
                int next = line.IndexOf(mark, at, StringComparison.Ordinal);
                if (!inComment) rest.Append(next < 0 ? line[at..] : line[at..next]);
                if (next < 0) break;
                inComment = !inComment;
                at = next + 2;
            }

            // What's left is a rule, or nothing.
            if (!string.IsNullOrWhiteSpace(rest.ToString())) count++;
        }

        return count;
    }

    /// <summary>What the build wrote (bin/, obj/) isn't ours to size.</summary>
    private static bool InBuildOutput(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}");
}
