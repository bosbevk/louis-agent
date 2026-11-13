namespace LouisAgent.Core.Tests.Docs.Instructions;

/// <summary>
/// CL63: a procedure Claude follows is a skill, a file loaded for its job (Louis: "separate functions by skill
/// files"). Claude Code shows every skill's description all the time and loads its body only when the job comes up.
/// So a skill is found by its description alone, and one without it would never load.
/// </summary>
public class SkillFilesTests
{
    /// <summary>The project's skills: each folder under <c>.claude/skills</c>.</summary>
    private static IReadOnlyList<string> SkillFolders()
    {
        string skills = Path.Combine(TestPaths.RepoRoot, ".claude/skills");
        return Directory.Exists(skills) ? Directory.EnumerateDirectories(skills).Order().ToList() : [];
    }

    /// <summary>
    /// What a skill file says about itself: the lines between its two <c>---</c> marks, as name and value. Empty
    /// when the file has no such block at its top.
    /// </summary>
    private static Dictionary<string, string> Says(string skillFile)
    {
        string[] lines = File.ReadAllLines(skillFile);
        if (lines.Length == 0 || lines[0].Trim() != "---") return [];

        return lines.Skip(1).TakeWhile(line => line.Trim() != "---")
            .Where(line => line.Contains(':') && !char.IsWhiteSpace(line[0]))
            .ToDictionary(line => line[..line.IndexOf(':')].Trim(), line => line[(line.IndexOf(':') + 1)..].Trim());
    }

    /// <summary>The project has skills at all. Without this, the rules below would pass on an empty folder.</summary>
    [Test]
    public void TheProject_HasSkills() =>
        Assert.That(SkillFolders(), Is.Not.Empty);

    /// <summary>Every skill folder holds its <c>SKILL.md</c>, the one file Claude Code looks for in it.</summary>
    [Test]
    public void EverySkillFolder_HoldsItsSkillFile()
    {
        var missing = SkillFolders().Where(folder => !File.Exists(Path.Combine(folder, "SKILL.md")))
            .Select(Path.GetFileName);

        Assert.That(missing, Is.Empty);
    }

    /// <summary>
    /// Every skill says when it applies, and is named as its folder is. The description is all Claude sees of a skill
    /// until it loads it, so it has to be there and say something.
    /// </summary>
    [Test]
    public void EverySkill_SaysWhenItApplies()
    {
        var wrong = SkillFolders()
            .Select(folder => (Name: Path.GetFileName(folder), Says: Says(Path.Combine(folder, "SKILL.md"))))
            .Where(skill => skill.Says.GetValueOrDefault("name") != skill.Name ||
                            skill.Says.GetValueOrDefault("description", "").Length < 40)
            .Select(skill => skill.Name);

        Assert.That(wrong, Is.Empty, "a skill's name is its folder's, and its description says when it applies");
    }

    /// <summary>
    /// The skills <c>CLAUDE.md</c> names and the skills there are, are the same list. <c>CLAUDE.md</c> is loaded on
    /// every turn and says which skill goes with which job. So a skill it doesn't name is one Claude isn't told to
    /// use, and a name with no skill behind it sends Claude to nothing. It names a skill as its command, in its
    /// <c>Skills</c> section: <c>`/draw-a-diagram`</c>.
    /// </summary>
    [Test]
    public void TheSkillsClaudeMdNames_AreTheSkillsThereAre()
    {
        string guide = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "src/docs/CLAUDE.md"));

        // Only its Skills section: the guide names other commands too, such as /mcp, which are not skills.
        int start = guide.IndexOf("## Skills", StringComparison.Ordinal);
        Assert.That(start, Is.GreaterThanOrEqualTo(0), "CLAUDE.md has a Skills section");
        int end = guide.IndexOf("\n## ", start + 1, StringComparison.Ordinal);
        string section = end < 0 ? guide[start..] : guide[start..end];

        // A skill's command there: a backtick, a slash, the name, a backtick.
        var named = section.Split('`')
            .Where(word =>
                word.StartsWith('/') && word.Length > 1 && word.Skip(1).All(c => char.IsLower(c) || c == '-'))
            .Select(word => word[1..])
            .Distinct()
            .Order();

        Assert.That(named, Is.EqualTo(SkillFolders().Select(Path.GetFileName)));
    }
}
