using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Skills;

// F21 step 1: a session names its personality; the prompt starts with that one instead of personality.md, and a name
// never reaches outside Skills/personalities/.
public class PersonalitiesTests
{
    private string _skills = null!;

    [SetUp]
    public void SetUp()
    {
        _skills = Path.Combine(Path.GetTempPath(), "puls-personalities-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_skills, Personalities.Folder));
        File.WriteAllText(Path.Combine(_skills, Personalities.Folder, "test-voice.md"), "# Test Voice\n\nKort en kragtig.\n");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skills, recursive: true);

    private AgentOptions Options() => new() { SkillsDirectory = _skills };

    [Test]
    public void NoName_IsTheDefaultPersonality()
    {
        string docs = AgentHost.LoadSkills(Options(), [], personality: null).Documentation;

        Assert.That(docs, Does.Contain("Louis's Minion"), "personality.md, as before F21");
        Assert.That(docs, Does.Not.Contain("# Test Voice"));
    }

    [Test]
    public void ANamedPersonality_ReplacesTheDefault_AndComesFirst()
    {
        string docs = AgentHost.LoadSkills(Options(), [], "test-voice").Documentation;

        Assert.That(docs, Does.Contain("# Test Voice"));
        Assert.That(docs, Does.Not.Contain("Louis's Minion"));
        Assert.That(docs.IndexOf("# Test Voice", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Louis Agent - Default Instructions", StringComparison.Ordinal)), "who the agent is, before the router");
    }

    [TestCase("../personality")]
    [TestCase("Puls")]
    [TestCase("a/b")]
    [TestCase("voice.md")]
    [TestCase("-voice")]
    public void ANameThatIsntValid_IsRefused(string name)
    {
        var ex = Assert.Throws<PersonalityException>(() => AgentHost.LoadSkills(Options(), [], name));
        Assert.That(ex!.Message, Does.Contain("lowercase letters, digits and dashes"));
    }

    [Test]
    public void AnUnknownName_IsRefused_ListingTheAvailable()
    {
        var ex = Assert.Throws<PersonalityException>(() => AgentHost.LoadSkills(Options(), [], "nobody"));
        Assert.That(ex!.Message, Does.Contain("Unknown personality 'nobody'").And.Contain("test-voice"));
    }

    [Test]
    public void Available_ListsTheFilesOnDisk()
    {
        Assert.That(Personalities.Available(_skills), Does.Contain("test-voice"));
    }

    [Test]
    public void PulsAndItsSpecialists_AreBuiltIn()
    {
        // F21 step 4: embedded, so a container without Skills/ still has them; a file on disk overrides.
        Assert.That(Personalities.Available(skillsDirectory: null), Is.SupersetOf(new[]
            { "puls", "puls-researcher", "puls-reader", "puls-historian", "puls-analyst", "puls-specialist" }));
        Assert.That(AgentHost.LoadSkills(new AgentOptions(), [], "puls").Documentation, Does.Contain("# Puls - Persoonlikheid"));
    }

    [TestCase(new[] { "web" }, "puls-researcher")]
    [TestCase(new[] { "files", "python" }, "puls-analyst")]
    [TestCase(new[] { "files", "git" }, "puls-historian")]
    [TestCase(new[] { "files" }, "puls-reader")]
    [TestCase(new[] { "dotnet" }, "puls-specialist")]
    public void ARoleComesFromTheToolsets_TheFirstRuleThatMatches(string[] toolsets, string role)
    {
        // The delegation check: Puls passed a role in one delegation of four; the peer now derives one (roles.json).
        string rules = SkillFileSource.ReadEmbedded(Personalities.RolesFile)!;

        Assert.That(Personalities.RoleFor(rules, toolsets), Is.EqualTo(role));
    }

    [Test]
    public void CountingGoesToTheAnalyst_AndTheReaderHandsItBack()
    {
        // FX13: a counting task sent to a files-only reader read every test file into a 298k-token prompt ($0.278).
        string router = SkillFileSource.ReadEmbedded("agents-skills.md")!;
        string reader = SkillFileSource.ReadEmbedded(Personalities.Folder + "/puls-reader.md")!;

        Assert.That(router, Does.Contain("Counting is the analyst's, never the reader's"));
        Assert.That(reader, Does.Contain("Don't count by reading").And.Contain("needs the analyst"));
    }

    [Test]
    public void UnreadableRules_GiveNoRole()
    {
        Assert.That(Personalities.RoleFor("not json", ["web"]), Is.Null);
    }
}
