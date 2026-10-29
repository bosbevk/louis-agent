namespace LouisAgent.Core.Tests.Docs;

/// <summary>
/// Keeps what Claude Code loads into its context short: every CLAUDE.md in the repo and every rules file in
/// .claude/rules/ has at most 200 lines, the size Claude Code's own guidance gives (Louis, 2026-10-29).
/// </summary>
/// <remarks>
/// A plain rule, not a ratchet: these files are few, so there is no list to shrink. What doesn't fit goes to an ADR
/// (the why), a path-scoped rules file (the how) or a guide in docs/guides/ (CL25).
/// </remarks>
public class ClaudeMdSizeTests
{
    private const int Limit = 200;

    /// <summary>
    /// Every CLAUDE.md and every rules file is at most 200 lines, blank lines counted: each line costs context.
    /// </summary>
    [Test]
    public void EveryClaudeFile_HoldsAtMost200Lines()
    {
        var over = ClaudeFiles()
            .Select(file => (File: Path.GetRelativePath(TestPaths.RepoRoot, file).Replace('\\', '/'),
                Lines: File.ReadAllLines(file).Length))
            .Where(f => f.Lines > Limit)
            .OrderBy(f => f.File)
            .Select(f => $"{f.File} ({f.Lines} lines)")
            .ToList();

        Assert.That(over, Is.Empty,
            $"A CLAUDE.md or a .claude/rules/ file holds at most {Limit} lines; every session loads it, so what " +
            "doesn't fit goes to an ADR, a rules file or a guide (CL25):\n  " + string.Join("\n  ", over));
    }

    /// <summary>The files Claude Code loads as instructions: every CLAUDE.md, and the rules files.</summary>
    private static IEnumerable<string> ClaudeFiles()
    {
        var claudeMds = Directory.EnumerateFiles(TestPaths.RepoRoot, "CLAUDE.md", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(p => p is "bin" or "obj" or "node_modules"));
        string rules = Path.Combine(TestPaths.RepoRoot, ".claude", "rules");
        var ruleFiles = Directory.Exists(rules) ? Directory.EnumerateFiles(rules, "*.md") : [];
        return claudeMds.Concat(ruleFiles);
    }
}
