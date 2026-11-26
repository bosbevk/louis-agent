using LouisAgent.Core.Skills.Headers.Agent;
using LouisAgent.Core.Skills.Headers.Agent.Library;

namespace LouisAgent.Core.Tests.Skills.Headers.Agent.Library;

/// <summary>
/// F97.1 step 6: the approved skills are every skill folder in the skills folder but <c>drafts/</c>, in name order, so
/// what a chat is shown is the same on every run. A folder that isn't a skill is passed over, and a skill that won't
/// read is left out, so one broken skill never keeps the others from a chat.
/// </summary>
public class SkillLibraryTests
{
    private string _skills = "";

    /// <summary>A skills folder of its own for each test.</summary>
    [SetUp]
    public void MakeTheSkillsFolder() => _skills = Directory.CreateTempSubdirectory("skill-library-").FullName;

    /// <summary>The folder goes with the test.</summary>
    [TearDown]
    public void RemoveTheSkillsFolder() => Directory.Delete(_skills, recursive: true);

    /// <summary>The good skills, by name; not a draft, a broken skill or a folder that holds no skill.</summary>
    [Test]
    public void TheApprovedSkills_AreTheGoodFolders_ByName()
    {
        Skill("word-count");
        Skill("char-count");
        Skill(Path.Combine("drafts", "line-count"));
        Skill("broken", withParameters: false);
        Directory.CreateDirectory(Path.Combine(_skills, "tools"));

        var library = SkillLibrary.Read(_skills);

        Assert.That(library.Approved.Select(s => (s.Name, s.State)),
            Is.EqualTo(new[] { ("char-count", SkillState.Approved), ("word-count", SkillState.Approved) }));
    }

    /// <summary>A skills folder that isn't there has no skills.</summary>
    [Test]
    public void NoSkillsFolder_HasNoSkills() =>
        Assert.That(SkillLibrary.Read(Path.Combine(_skills, "none")).Approved, Is.Empty);

    /// <summary>A skill's folder, good unless told to leave out its parameters.</summary>
    private void Skill(string folder, bool withParameters = true)
    {
        string path = Path.Combine(_skills, folder);
        string name = Path.GetFileName(folder);
        Directory.CreateDirectory(Path.Combine(path, "scripts"));
        File.WriteAllText(Path.Combine(path, "SKILL.md"),
            $"---\nname: {name}\ndescription: Counts.\nmetadata:\n  script: scripts/run.py\n---\n# Count\n");
        File.WriteAllText(Path.Combine(path, "scripts", "run.py"), "print(1)");
        if (!withParameters) return;

        File.WriteAllText(Path.Combine(path, "parameters.schema.json"), """{ "type": "object", "properties": {} }""");
    }
}
