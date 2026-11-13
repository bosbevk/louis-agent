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
}
