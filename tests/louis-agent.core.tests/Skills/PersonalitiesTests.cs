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
            { "puls", "puls-researcher", "puls-reader", "puls-historian", "puls-analyst", "puls-specialist", "puls-cook" }));
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
        Assert.That(router, Does.Contain("Every sub-session costs money"), "the one choosing is told the cost too");
        Assert.That(reader, Does.Contain("Don't count by reading").And.Contain("needs the analyst"));
    }

    [TestCase("puls-researcher")]
    [TestCase("puls-reader")]
    [TestCase("puls-historian")]
    [TestCase("puls-analyst")]
    [TestCase("puls-specialist")]
    [TestCase("puls-cook")]
    public void EverySpecialist_IsToldWhatItsToolCallsCost(string role)
    {
        // FX13: the reader that read 74 files never saw the router; the rule has to be in the role it runs with.
        string text = SkillFileSource.ReadEmbedded($"{Personalities.Folder}/{role}.md")!;

        Assert.That(text, Does.Contain("Use the fewest tool calls that answer the task").And.Contain("paid for again"));
    }

    [Test]
    public void TheCook_AnswersForWhereTheUserIs_AndNeverInventsAPrice()
    {
        // The local-specialist idea: one question gets a South African recipe, where to buy it and what it costs.
        string cook = SkillFileSource.ReadEmbedded(Personalities.Folder + "/puls-cook.md")!;

        Assert.That(cook, Does.Contain("Where the user is").And.Contain("Never invent a price"));
    }

    [Test]
    public void AFollowUp_GoesToTheOpenSpecialist()
    {
        // F26 step 4: the tool exists from the first turn; the prompt says when to continue and when to start over.
        string agents = SkillFileSource.ReadEmbedded("agents-skills.md")!;
        string router = SkillFileSource.ReadEmbedded("default.md")!;

        Assert.That(agents, Does.Contain("## Following up").And.Contain("`ContinueSubSession(handle, message, attachments)`"));
        Assert.That(agents, Does.Contain("start a new sub-session").IgnoreCase, "and when not to continue");
        Assert.That(router, Does.Contain("ContinueSubSession"));
    }

    [TestCase("default.md")]
    [TestCase("agents-skills.md")]
    public void AMealToCook_GoesToTheCook_WithWeb(string router)
    {
        // Puls routes by its tables (default.md says which toolset and role; agents-skills.md which role per toolset).
        // Without a row in both, "what should I cook?" is just another web question and goes to the researcher.
        string[] rows = SkillFileSource.ReadEmbedded(router)!.Split('\n');
        string? row = rows.FirstOrDefault(r => r.StartsWith('|') && r.Contains("`puls-cook`"));

        Assert.That(row, Is.Not.Null, $"{router} has no table row for the cook");
        Assert.That(row, Does.Contain("`web`").And.Contain("cook").IgnoreCase);
    }

    [Test]
    public void UnreadableRules_GiveNoRole()
    {
        Assert.That(Personalities.RoleFor("not json", ["web"]), Is.Null);
    }
}
