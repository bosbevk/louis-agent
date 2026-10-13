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
}
