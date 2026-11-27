using LouisAgent.Core.Prompting.Headers.Agent;
using LouisAgent.Core.Prompting.Headers.Agent.Library;

namespace LouisAgent.Core.Tests.Prompting.Headers.Agent.Library;

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

    /// <summary>
    /// F97.1 step 9: a chat finds every approved skill and its own drafts, never another chat's draft, by the names of
    /// the drafts it made.
    /// </summary>
    [Test]
    public void AChat_FindsTheApprovedSkills_AndOnlyItsOwnDrafts()
    {
        Skill("word-count");
        Skill(Path.Combine("drafts", "line-count"));
        Skill(Path.Combine("drafts", "char-count"));
        var library = SkillLibrary.Read(_skills);
        var mine = new HashSet<string> { "line-count" };

        Assert.Multiple(() =>
        {
            Assert.That(library.Find("word-count", mine)?.State, Is.EqualTo(SkillState.Approved));
            Assert.That(library.Find("line-count", mine)?.State, Is.EqualTo(SkillState.Drafted));
            Assert.That(library.Find("char-count", mine), Is.Null, "another chat's draft");
            Assert.That(library.Find("nothing", mine), Is.Null);
        });
    }

    /// <summary>
    /// F97.1 step 11: approving a draft moves its folder up, so it is approved at once, for every chat; rejecting one
    /// deletes it; a name with no draft is left to the old commands.
    /// </summary>
    [Test]
    public void Approving_MovesTheDraftUp_AndRejecting_DeletesIt()
    {
        Skill(Path.Combine("drafts", "line-count"));
        Skill(Path.Combine("drafts", "char-count"));
        var library = SkillLibrary.Read(_skills);

        string? approved = library.Approve("line-count");
        string? rejected = library.Reject("char-count");

        Assert.Multiple(() =>
        {
            Assert.That(approved, Does.Contain("line-count"));
            Assert.That(rejected, Does.Contain("char-count"));
            Assert.That(library.Approved.Select(s => s.Name), Is.EqualTo(new[] { "line-count" }));
            Assert.That(Directory.Exists(Path.Combine(_skills, "drafts", "line-count")), Is.False);
            Assert.That(Directory.Exists(Path.Combine(_skills, "drafts", "char-count")), Is.False);
            Assert.That((library.Approve("none"), library.Reject("none")), Is.EqualTo(((string?)null, (string?)null)));
        });
    }

    /// <summary>The drafts waiting on disk, each with its example, for <c>/skills</c>.</summary>
    [Test]
    public void TheDrafts_AreTheOnesWaiting()
    {
        Skill(Path.Combine("drafts", "line-count"));

        Assert.That(SkillLibrary.Read(_skills).Drafts.Select(s => s.Name), Is.EqualTo(new[] { "line-count" }));
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
