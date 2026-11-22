using LouisAgent.Choices;
using LouisAgent.Composition.Prompts;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Skills.Roles;

// F21 step 1: a session names its role (its personality before CL13); the prompt starts with that one instead of
// personality.md, and a name never reaches outside its level's folder (CL8: Skills/agent/, Skills/subagents/).
// CL15 step 3: the mechanism is tested on the tests' own roles (agents/testprod and the rest); Puls's content is
// tested with Puls, in its identity's tests.
public class RoleCatalogTests
{
    private const string Modes =
        "testprod/voice-af, testprod/voice-en";

    private string _skills = null!;

    [SetUp]
    public void SetUp()
    {
        _skills = Path.Combine(Path.GetTempPath(), "roles-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_skills, PromptFolder.Agents.Wire()));
        File.WriteAllText(Path.Combine(_skills, PromptFolder.Agents.Wire(), "test-voice.md"),
            "# Test Voice\n\nKort en kragtig.\n");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skills, recursive: true);

    private AgentOptions Options() => new()
    {
        SkillsDirectory = _skills,
        DefaultVoice = "testprod/voice-en"
    };

    [Test]
    public void NoName_IsTheDefaultMode()
    {
        string docs = SkillLoader.Load(Options(), [], role: null).Documentation;

        Assert.That(docs, Does.Contain("# Testprod - Voice"),
            "the identity's default voice");
        Assert.That(docs, Does.Not.Contain("# Testprod - Stem"));
        Assert.That(docs, Does.Contain("# Testprod - Routing"), "and its router");
    }

    [Test]
    public void AMode_IsOneFile_ItsVoiceThenItsRouting()
    {
        // CL9: a level-0 session loads one file; its voice frames everything after it, its routing follows.
        string docs = SkillLoader.Load(new AgentOptions { DefaultVoice = "testprod/voice-en" }, [], "testprod/voice-af")
            .Documentation;

        Assert.That(docs.IndexOf("# Testprod - Stem", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Testprod - Routing", StringComparison.Ordinal)));
        Assert.That(Body("agents/testprod/voice.md"), Does.StartWith("# Testprod - Routing"),
            "the product's shared part");
    }

    [Test]
    public void ANamedRole_ReplacesTheDefault_AndComesFirst()
    {
        string docs = SkillLoader.Load(Options(), [], "testprod/voice-af").Documentation;

        Assert.That(docs, Does.Contain("# Testprod - Stem"));
        Assert.That(docs, Does.Not.Contain("# Testprod - Voice"));
        Assert.That(docs.IndexOf("# Testprod - Stem", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Testprod - Routing", StringComparison.Ordinal)),
            "who the agent is, before its routing");
    }

    [TestCase("../personality")]
    [TestCase("Testprod")]
    [TestCase("a/b/c")]
    [TestCase("a.b.c")]
    [TestCase("-voice")]
    public void ANameThatIsntValid_IsRefused(string name)
    {
        var ex = Assert.Throws<RoleException>(() => SkillLoader.Load(Options(), [], name));
        Assert.That(ex!.Message, Does.Contain("lowercase letters, digits and dashes"));
    }

    [Test]
    public void AnUnknownName_IsRefused_ListingTheAvailable()
    {
        var ex = Assert.Throws<RoleException>(() => SkillLoader.Load(Options(), [], "nobody"));
        Assert.That(ex!.Message, Does.Contain("Unknown role 'nobody'").And.EndWith($"Available: {Modes}."));
    }

    [Test]
    public void AVoiceOnDisk_IsNeitherOfferedNorLoaded()
    {
        // CL8 step 4: roles are built in; the skills folder holds only what the agent writes.
        Assert.That(RoleCatalog.Available(RoleLevel.Agent), Does.Not.Contain("test-voice"));
        Assert.Throws<RoleException>(() => SkillLoader.Load(Options(), [], "test-voice"));
    }

    [Test]
    public void Core_EmbedsNoRoleOfAProduct_UntilAnIdentityBringsIt()
    {
        // CL15 step 3 (the cut): Puls's modes, subagents and lookup agents live in its identity. Without it registered
        // (these tests register only their own roles) no level lists a Puls role; the identity's tests show the other
        // side.
        foreach (var level in Enum.GetValues<RoleLevel>())
            Assert.That(RoleCatalog.Available(level), Has.None.StartsWith("puls"), $"level {level}");
        Assert.That(SkillFileSource.CoreFiles, Has.None.Contains("puls"), "core's own prompts name no product file");
        Assert.Throws<RoleException>(() =>
            SkillLoader.Load(new AgentOptions { DefaultVoice = "testprod/voice-en" }, [], "puls/voice-af"));
    }

    [TestCase("af", "# Testprod - Stem")]
    [TestCase("en", "# Testprod - Voice")]
    public void ALanguage_LoadsItsVoice_ThenTheProductsShared(string code, string voice)
    {
        // CL9 step 6 (Louis: "a puls/voice.md for shared things"): the language file says how the product talks,
        // voice.md routes.
        string docs = SkillLoader
            .Load(new AgentOptions { DefaultVoice = "testprod/voice-en" }, [], $"testprod/voice-{code}").Documentation;

        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal), Is.GreaterThan(0));
        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Testprod - Routing", StringComparison.Ordinal)));
        Assert.That(RoleCatalog.LanguageOf($"testprod/voice-{code}"), Is.EqualTo(code));
    }

    [Test]
    public void AProductsSharedVoice_IsNotAModeOnItsOwn()
    {
        Assert.That(RoleCatalog.Available(RoleLevel.Agent), Is.EqualTo(Modes.Split(", ")));
        Assert.Throws<RoleException>(() =>
            SkillLoader.Load(new AgentOptions { DefaultVoice = "testprod/voice-en" }, [], "testprod/voice"));
        Assert.That(RoleCatalog.LanguageOf("louis-agent/voice"), Is.Null, "the coding agent's mode is whole");
    }

    [TestCase("functions/code-review")]
    [TestCase("testprod")]
    [TestCase("default")]
    public void OnlyAMode_IsANameAtLevelZero(string name)
    {
        // CL9: a mode is agent/{product}/{mode}.md; a function skill, a product folder or an old name isn't one.
        var ex = Assert.Throws<RoleException>(() =>
            SkillLoader.Load(new AgentOptions { DefaultVoice = "testprod/voice-en" }, [], name));

        Assert.That(ex!.Message, Does.EndWith($"Available: {Modes}."));
    }

    [Test]
    public void EachLevel_ListsOnlyItsOwnRoles()
    {
        // CL8: each level lists only its own; a lookup agent (level 2) lives in its agent's lookups/ (CL68.3).
        Assert.That(RoleCatalog.Available(RoleLevel.Subagent), Is.EqualTo(new[] { "testprod.researcher" }));
        Assert.That(RoleCatalog.Available(RoleLevel.Lookup), Is.EqualTo(new[] { "testprod.lookup" }));
    }

    /// <summary>
    /// A subagent's name is its address, one dot between its identity and itself (CL68.2, ADR 0133), and only the
    /// catalogue knows which file holds it; a second dot, or a dot beside a slash, names nothing.
    /// </summary>
    [Test]
    public void ASubagentsName_IsItsAddress_AndTheCatalogueFindsItsFile()
    {
        Assert.That(new[] { "testprod.researcher", "acme.cook", "acme/voice-en" }.All(RoleCatalog.IsValidName));
        Assert.That(new[] { "a.b.c", "a.b/c", ".cook", "acme." }.Any(RoleCatalog.IsValidName), Is.False);

        Assert.That(RoleCatalog.FileName("testprod.researcher", RoleLevel.Subagent),
            Is.EqualTo("agents/testprod/subagents/researcher/voice.md"));
    }

    [Test]
    public void ASubagentsRole_IsRefusedAtLevelZero_NamingTheVoicesThereAre()
    {
        // CL8: a role opens only at its own level, so a chat can't take a subagent's role as its voice.
        var ex = Assert.Throws<RoleException>(() =>
            SkillLoader.Load(new AgentOptions { DefaultVoice = "testprod/voice-en" }, [], "testprod.researcher"));

        Assert.That(ex!.Message,
            Does.Contain("Unknown role 'testprod.researcher'").And.EndWith($"Available: {Modes}."));
    }

    [Test]
    public void TheAgentsVoice_IsRefusedAtLevelOne_NamingTheRolesThereAre()
    {
        var ex = Assert.Throws<RoleException>(() =>
            SkillLoader.Load(new AgentOptions { DefaultVoice = "testprod/voice-en" }, [], "testprod/voice-af",
                RoleLevel.Subagent));

        Assert.That(ex!.Message, Does.Contain("Unknown role 'testprod/voice-af'").And.Contain("testprod.researcher"));
    }

    [Test]
    public void OnlyLevelZero_HasTheRouter()
    {
        // The router picks toolsets and delegates; a subagent's toolsets were chosen for it (F21), and so are a
        // lookup agent's.
        Assert.That(
            SkillLoader.Load(new AgentOptions { DefaultVoice = "testprod/voice-en" }, [], level: RoleLevel.Agent)
                .Documentation,
            Does.Contain("# Testprod - Routing"));
        Assert.That(
            SkillLoader.Load(new AgentOptions { DefaultVoice = "testprod/voice-en" }, [], level: RoleLevel.Subagent)
                .Documentation,
            Does.Not.Contain("# Testprod - Routing"));
    }

    /// <summary>
    /// The guide every identity that asks other agents reads (F127.3.1) says how to ask, follow up and read the work,
    /// and names no product: a product's briefing and its subagents stay in its own voice, as Puls's do. The
    /// subagents' guide it replaced is gone.
    /// </summary>
    [Test]
    public void TheAgentsGuide_IsTheMechanism_NotAProductsProtocol()
    {
        string guide = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;

        Assert.Multiple(() =>
        {
            Assert.That(guide, Does.Contain("`AskAgent(address, message)`").And.Contain("`ReadAgentWork(address)`"));
            Assert.That(guide, Does.Contain("Asking the same address again carries on"));
            Assert.That(guide, Does.Not.Contain("puls").IgnoreCase.And.Not.Contain("seemeeu").IgnoreCase);
            Assert.That(SkillFileSource.ReadEmbedded("toolsets/subagents.md"), Is.Null);
            Assert.That(SkillFileSource.ReadEmbedded("subagents/roles.json"), Is.Null,
                "CL10: a role is named, never derived");
        });
    }

    /// <summary>
    /// A lookup agent is asked through the agents guide's <c>AskAgent</c>, with its tool (F127.5), and the guide names
    /// no product's role; the lookup agents guide it folded into is gone.
    /// </summary>
    [Test]
    public void TheAgentsGuide_SaysHowToAskALookupAgent_AndNamesNoProductsRole()
    {
        string guide = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;

        Assert.That(guide, Does.Contain("`AskAgent(address, message, tool, stronger)`"));
        Assert.That(guide, Does.Not.Contain("puls").IgnoreCase.And.Not.Contain("Malmesbury"));
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/lookup agents.md"), Is.Null);
    }

    public void AVoicesHeading_SaysVoice(string file, string heading) =>
        Assert.That(Body(file), Does.StartWith(heading));

    /// <summary>A built-in prompt as the model reads it, without its header (CL27).</summary>
    private static string Body(string path) => PromptHeaders.Split(path, SkillFileSource.ReadEmbedded(path)!).Body;
}
