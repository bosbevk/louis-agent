namespace LouisAgent.Core.Tests;

/// <summary>
/// Keeps files small enough to read: at most 200 lines of code, a test file 400 (blank and comment lines don't count).
/// A bigger class is split by what it does (src/docs/CLAUDE.md, *Separate by concern*).
/// </summary>
/// <remarks>
/// A ratchet (S, CL15): the files over their limit today are listed in Oversized, a new one fails the test, and a
/// listed file that has come under its limit fails too, so the list can only shrink until it is deleted and the test
/// is a plain rule. Counted from the source files, bin/ and obj/ skipped.
/// </remarks>
/// <seealso href="https://github.com/bosbevk/louis-agent/blob/main/src/docs/adr/0044-the-size-limit-counts-code.md">ADR 0044: the size limit counts code</seealso>
public class FileSizeTests
{
    private const int SourceLimit = 200;
    private const int TestLimit = 400;

    private static readonly string[] SourceRoots =
    [
        "src", "packs", "demos"
    ];

    /// <summary>
    /// Repo-relative, forward slashes, generated from the tree on 2026-10-20; remove a file's line in the commit that
    /// splits it.
    /// </summary>
    private static readonly HashSet<string> Oversized =
    [
        "src/engine/louis-agent.core/Agent/EngineTools/AgentBuiltTools.cs",
        "src/engine/louis-agent.core/Agent/EngineTools/SkillTools.cs",
        "packs/tools/louis-agent.pack.dotnet/DotNetTools.cs",
        "packs/tools/louis-agent.pack.devops/DevOpsTools.cs",
        "packs/tools/louis-agent.pack.paymo/PaymoTools.cs",
        "src/engine/louis-agent.core/Web/Browsing/ChromeWebBrowser.cs",
        "packs/tests/louis-agent.pack.devops.tests/DevOpsToolsTests.cs",
        "packs/tests/louis-agent.pack.paymo.tests/PaymoToolsTests.cs",
        "src/tools/louis-agent.usage-probe/Program.cs",
    ];

    /// <summary>
    /// No file is over its limit unless it is still listed, and a listed file that has come under it is taken off.
    /// </summary>
    [Test]
    public void AFile_HoldsAtMost200Lines_ATestFile400_UnlessStillOversized()
    {
        var over = new HashSet<string>();
        foreach (string root in SourceRoots)
        {
            string dir = Path.Combine(TestPaths.RepoRoot, root);
            if (!Directory.Exists(dir)) continue;
            foreach (string file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                // Only what we wrote and know how to size: C#, markup and stylesheets.
                if (InBuildOutput(file) || Lines(file) is not { } lines) continue;
                string relative = Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/');
                if (lines > (TestPaths.InTestProject(relative) ? TestLimit : SourceLimit)) over.Add(relative);
            }
        }

        var newOffenders = over.Except(Oversized).Order().ToList();
        var shrunk = Oversized.Except(over).Order().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(newOffenders, Is.Empty,
                $"A file holds at most {SourceLimit} lines (a test file {TestLimit}): split by responsibility "
                + "(src/docs/CLAUDE.md, Separate by concern):\n  "
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
        ".css" or ".scss" => CountCssLines(File.ReadAllLines(file)),
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

    /// <summary>A web app on Sass gets its CSS from the build, themes too; its Sass is what's sized.</summary>
    private static bool IsMadeFromSass(string path)
    {
        int css = path.IndexOf(
            $"{Path.DirectorySeparatorChar}wwwroot{Path.DirectorySeparatorChar}css{Path.DirectorySeparatorChar}",
            StringComparison.Ordinal);
        return css >= 0 && path.EndsWith(".css", StringComparison.Ordinal)
                        && File.Exists(Path.Combine(path[..css], "Styles", "app.scss"));
    }

    /// <summary>
    /// What a tool wrote (bin/, obj/, the CSS it makes from Sass, an EF Core migration and its snapshot) isn't ours to
    /// size: nobody maintains it by hand (F61.1).
    /// </summary>
    private static bool InBuildOutput(string path) =>
        IsMadeFromSass(path)
        || path.Contains($"{Path.DirectorySeparatorChar}Migrations{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}data{Path.DirectorySeparatorChar}")
        || path.Contains($"{Path.DirectorySeparatorChar}logs{Path.DirectorySeparatorChar}");
}
