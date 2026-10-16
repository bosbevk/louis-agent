using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;

namespace LouisAgent.Core.Tests.Skills;

// F21 step 1: a session names its personality; the prompt starts with that one instead of personality.md, and a name
// never reaches outside its level's folder (CL8: Skills/agent/, Skills/specialists/).
public class PersonalitiesTests
{
    private string _skills = null!;

    [SetUp]
    public void SetUp()
    {
        _skills = Path.Combine(Path.GetTempPath(), "puls-personalities-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_skills, Personalities.AgentFolder));
        File.WriteAllText(Path.Combine(_skills, Personalities.AgentFolder, "test-voice.md"), "# Test Voice\n\nKort en kragtig.\n");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skills, recursive: true);

    private AgentOptions Options() => new() { SkillsDirectory = _skills };

    [Test]
    public void NoName_IsTheDefaultPersonality()
    {
        string docs = AgentHost.LoadSkills(Options(), [], personality: null).Documentation;

        Assert.That(docs, Does.Contain("Louis's Minion"), "the default voice, louis-agent/voice (personality.md before CL9)");
        Assert.That(docs, Does.Not.Contain("# Puls - Persoonlikheid"));
        Assert.That(docs, Does.Contain("# Louis Agent - Default Instructions"), "and its router");
    }

    [Test]
    public void AMode_IsOneFile_ItsVoiceThenItsRouting()
    {
        // CL9: a level-0 session loads one file; its voice frames everything after it, its routing follows.
        string docs = AgentHost.LoadSkills(new AgentOptions(), [], "puls/voice-af").Documentation;

        Assert.That(Personalities.DefaultVoice, Is.EqualTo("louis-agent/voice"));
        Assert.That(docs.IndexOf("# Puls - Persoonlikheid", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Louis Agent - Default Instructions", StringComparison.Ordinal)));
        var (voice, routing) = Personalities.ModeParts(SkillFileSource.ReadEmbedded("agent/puls/voice-af.md")!);
        Assert.That((voice.StartsWith("# Puls"), routing.StartsWith("# Louis Agent - Default Instructions")), Is.EqualTo((true, true)));
    }

    [Test]
    public void ANamedPersonality_ReplacesTheDefault_AndComesFirst()
    {
        string docs = AgentHost.LoadSkills(Options(), [], "puls/voice-af").Documentation;

        Assert.That(docs, Does.Contain("# Puls - Persoonlikheid"));
        Assert.That(docs, Does.Not.Contain("Louis's Minion"));
        Assert.That(docs.IndexOf("# Puls - Persoonlikheid", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Louis Agent - Default Instructions", StringComparison.Ordinal)), "who the agent is, before the router");
    }

    [TestCase("../personality")]
    [TestCase("Puls")]
    [TestCase("a/b/c")]
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
        Assert.That(ex!.Message, Does.Contain("Unknown personality 'nobody'").And.EndWith("Available: louis-agent/voice, puls/voice-af."));
    }

    [Test]
    public void AVoiceOnDisk_IsNeitherOfferedNorLoaded()
    {
        // CL8 step 4: personalities are built in; the skills folder holds only what the agent writes.
        Assert.That(Personalities.Available(SkillLevel.Agent), Does.Not.Contain("test-voice"));
        Assert.Throws<PersonalityException>(() => AgentHost.LoadSkills(Options(), [], "test-voice"));
    }

    [Test]
    public void PulsAndItsSpecialists_AreBuiltIn()
    {
        // F21 step 4: embedded, so a container without Skills/ still has them; a file on disk overrides.
        Assert.That(Personalities.Available(SkillLevel.Agent), Is.EqualTo(new[] { "louis-agent/voice", "puls/voice-af" }));
        Assert.That(Personalities.Available(SkillLevel.Specialist), Is.SupersetOf(new[]
            { "puls-researcher", "puls-reader", "puls-historian", "puls-analyst", "puls-specialist", "puls-cook" }));
        Assert.That(AgentHost.LoadSkills(new AgentOptions(), [], "puls/voice-af").Documentation, Does.Contain("# Puls - Persoonlikheid"));
        Assert.That(AgentHost.LoadSkills(new AgentOptions(), [], "puls-cook", SkillLevel.Specialist).Documentation,
            Does.Contain("Never invent a price"));
    }

    [TestCase("functions/code-review")]
    [TestCase("puls")]
    [TestCase("default")]
    public void OnlyAMode_IsANameAtLevelZero(string name)
    {
        // CL9: a mode is agent/{product}/{mode}.md; a function skill, a product folder or an old name isn't one.
        var ex = Assert.Throws<PersonalityException>(() => AgentHost.LoadSkills(new AgentOptions(), [], name));

        Assert.That(ex!.Message, Does.EndWith("Available: louis-agent/voice, puls/voice-af."));
    }

    [Test]
    public void EachLevel_ListsOnlyItsOwnPersonalities()
    {
        // CL8: the folders follow the levels; minions (level 2) are F30's, so its folder is empty until then.
        Assert.That(Personalities.Available(SkillLevel.Specialist), Does.Not.Contain("puls/voice-af"));
        Assert.That(Personalities.Available(SkillLevel.Minion), Is.Empty);
    }

    [Test]
    public void ASpecialistsRole_IsRefusedAtLevelZero_NamingTheVoicesThereAre()
    {
        // CL8: a role opens only at its own level, so Puls's chat can't take the cook's role as its voice.
        var ex = Assert.Throws<PersonalityException>(() => AgentHost.LoadSkills(new AgentOptions(), [], "puls-cook", SkillLevel.Agent));

        Assert.That(ex!.Message, Does.Contain("Unknown personality 'puls-cook'").And.EndWith("Available: louis-agent/voice, puls/voice-af."));
    }

    [Test]
    public void TheAgentsVoice_IsRefusedAtLevelOne_NamingTheRolesThereAre()
    {
        var ex = Assert.Throws<PersonalityException>(() => AgentHost.LoadSkills(new AgentOptions(), [], "puls/voice-af", SkillLevel.Specialist));

        Assert.That(ex!.Message, Does.Contain("Unknown personality 'puls/voice-af'").And.Contain("puls-cook"));
    }

    [Test]
    public void OnlyLevelZero_HasTheRouter()
    {
        // The router picks toolsets and delegates; a specialist's toolsets were chosen for it (F21), and so are a minion's.
        Assert.That(AgentHost.LoadSkills(new AgentOptions(), [], level: SkillLevel.Agent).Documentation,
            Does.Contain("# Louis Agent - Default Instructions"));
        Assert.That(AgentHost.LoadSkills(new AgentOptions(), [], level: SkillLevel.Specialist).Documentation,
            Does.Not.Contain("# Louis Agent - Default Instructions"));
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
        string router = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;
        string reader = SkillFileSource.ReadEmbedded(Personalities.SpecialistsFolder + "/puls-reader.md")!;

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
        string text = SkillFileSource.ReadEmbedded($"{Personalities.SpecialistsFolder}/{role}.md")!;

        Assert.That(text, Does.Contain("Use the fewest tool calls that answer the task").And.Contain("paid for again"));
    }

    [Test]
    public void TheCook_AnswersForWhereTheUserIs_AndNeverInventsAPrice()
    {
        // The local-specialist idea: one question gets a South African recipe, where to buy it and what it costs.
        string cook = SkillFileSource.ReadEmbedded(Personalities.SpecialistsFolder + "/puls-cook.md")!;

        Assert.That(cook, Does.Contain("Where the user is").And.Contain("Never invent a price"));
    }

    [Test]
    public void AFollowUp_GoesToTheOpenSpecialist()
    {
        // F26 step 4: the tool exists from the first turn; the prompt says when to continue and when to start over.
        string agents = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice-af.md")!;

        Assert.That(agents, Does.Contain("## Following up").And.Contain("`ContinueSubSession(handle, message, attachments)`"));
        Assert.That(agents, Does.Contain("start a new sub-session").IgnoreCase, "and when not to continue");
        Assert.That(router, Does.Contain("ContinueSubSession"));
        // The cook check: Puls scaled the list itself in half the follow-ups, reading the rule as optional. Firmer now.
        Assert.That(router, Does.Contain("even when you could answer it yourself"));
        Assert.That(agents, Does.Contain("Continue, don't answer it yourself"));
    }

    [Test]
    public void EveryCookingQuestion_GoesToTheCook_WhichSaysWhereToBuy()
    {
        // Louis's manual test: "ek wil bebotie maak, wat het ek nodig" was answered from memory, no prices, no shop.
        // Cooking is the cook's even when Puls knows the recipe, and the cook names the branches near the user.
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice-af.md")!;
        string agents = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;
        string cook = SkillFileSource.ReadEmbedded(Personalities.SpecialistsFolder + "/puls-cook.md")!;

        Assert.That(router, Does.Contain("every cooking question").And.Contain("never from memory"));
        Assert.That(agents, Does.Contain("every cooking question"));
        Assert.That(cook, Does.Contain("Where to buy").And.Contain("branches"));
        // Louis's second test: the shops' store finders and search pages are JavaScript and gave the cook nothing, while
        // OpenStreetMap's search returns the branches as JSON; the role names it, and says which pages not to fetch.
        Assert.That(cook, Does.Contain("nominatim.openstreetmap.org/search").And.Contain("Don't fetch"));
        // Louis: a rating, how many ingredients got a price. The cook states it in a fixed line the check reads.
        Assert.That(cook, Does.Contain("Geprys: {n} van {m} bestanddele"));
    }

    [Test]
    public void TheCook_WritesItsReportInOneShape_ThatPulsHandsOn()
    {
        // Louis: "force Puls to output the data as the cook said it, and conform the cook's output". The report is
        // written for the user in a fixed shape (headings in order, the list and the branches as tables with fixed
        // columns, a price only with its source), so Puls can hand it on whole and the check can read it row by row.
        string cook = SkillFileSource.ReadEmbedded(Personalities.SpecialistsFolder + "/puls-cook.md")!;

        Assert.That(cook, Does.Contain("Puls hands your report to the user as it is"));
        Assert.That(cook, Does.Not.Contain("Puls will put it in its own voice"), "the line every other role has invites a rewrite");
        // Louis: one table, not the recipe's ingredients and then the list again.
        // Louis: two shops' prices per ingredient, and the totals merged into one table that compares them.
        Assert.That(cook, Does.Contain("| Ingredient | For the recipe | To buy | Checkers | Shoprite | Pick n Pay | Best price |"), "the shopping list's columns: a column per shop priced at");
        // Louis: "one shop has to at least have all products; include Spar, Pick n Pay, Usave and Woolworths, it never uses them".
        Assert.That(cook, Does.Contain("at least one shop must have").And.Contain("one trip"));
        Assert.That(cook, Does.Contain("Pick n Pay, Spar, Usave, Woolworths").And.Contain("`pnp.co.za`").And.Contain("`usave.co.za`"));
        // The cheapest basket can exceed a shop's total that leaves rows out, so it's bounded by the completed column.
        Assert.That(cook, Does.Contain("the cheapest basket is never more than the completed"), "the best-price total the model got wrong");
        Assert.That(cook, Does.Not.Contain("Checkers and Shoprite always"), "the two shops are picked at random from the town's");
        // Louis: "pick randomly which store to use based on location; don't even use stores not in your location".
        Assert.That(cook, Does.Contain("q=supermarkets+in+Stellenbosch").And.Contain("pick two at random").And.Contain("never price at, name or total a shop the town doesn't have"));
        Assert.That(cook, Does.Contain("| **Total, priced rows** | | | **R102,98** | **R84,99** | **R120,97** | **R110,97** |"), "the totals are the list's last rows, side by side, the cheapest basket last");
        // Louis: an unpriced ingredient is an ordinary row that says unknown; no row counting or naming them.
        Assert.That(cook, Does.Contain("| Lemons | 2 | 2 | [R12,99](https://www.checkers.co.za/…) | unknown | [R14,99](https://www.pnp.co.za/…) | R12,99 Checkers |"));
        Assert.That(cook, Does.Not.Contain("**Unpriced"));
        Assert.That(cook, Does.Not.Contain("### Total"), "no section of its own");
        Assert.That(cook, Does.Contain("never a price without its link"), "the source is the link in the cell");
        Assert.That(cook, Does.Contain("don't list them here too"));
        // Louis: where to buy links the shop's site, the branch's own page when it has one.
        Assert.That(cook, Does.Contain("| Shop | Branch | Street or mall | Website |"), "the branches' fixed columns");
        Assert.That(cook, Does.Contain("the branch's own page on the shop's site").And.Contain("else the shop's home page"));
        // Louis: the list, the branches, the total and the doubts all as tables.
        Assert.That(cook, Does.Contain("| What | Why |"), "the doubts' fixed columns");
        int dish = cook.IndexOf("### The dish", StringComparison.Ordinal), list = cook.IndexOf("### Shopping list", StringComparison.Ordinal),
            buy = cook.IndexOf("### Where to buy", StringComparison.Ordinal), unsure = cook.IndexOf("### Unsure", StringComparison.Ordinal);
        Assert.That(dish, Is.GreaterThan(-1).And.LessThan(list), "the headings, in the report's order");
        Assert.That(list, Is.LessThan(buy));
        Assert.That(buy, Is.LessThan(unsure));
    }

    [Test]
    public void Puls_RelaysTheCooksReport_InsteadOfRewritingIt()
    {
        // Louis's manual test after the cook's template: "the cook's output differed from Puls". Nothing told Puls to
        // hand a report on; the router said "pass the result on" and the roles "Puls will put it in its own voice".
        string agents = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice-af.md")!;

        Assert.That(agents, Does.Contain("## Relaying a report"));
        Assert.That(agents, Does.Contain("as the specialist wrote it").And.Contain("every table row"), "whole, tables included");
        Assert.That(router, Does.Contain("as the cook wrote it"), "the one choosing is told too");
    }

    [Test]
    public void Puls_HandsOffLikeAPerson_AndIntroducesTheReport()
    {
        // Louis: "say something like I'm handing this to my specialist, back shortly" before the call, and "my cook
        // just came back with this" above the report. The voice file said the opposite: never forward a report as is.
        string voice = SkillFileSource.ReadEmbedded(Personalities.AgentFolder + "/puls/voice-af.md")!;
        string agents = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice-af.md")!;

        Assert.That(voice, Does.Not.Contain("Moet nooit 'n spesialis se verslag net so aanstuur nie"));
        Assert.That(voice, Does.Contain("my kok").And.Contain("nou-nou terug"), "the hand-off line, in Puls's voice");
        Assert.That(agents, Does.Contain("before the call").IgnoreCase.And.Contain("back shortly"), "one line to the user, then the tool call");
        Assert.That(agents, Does.Contain("just came back"), "the line above the report introduces who came back with it");
        Assert.That(router, Does.Contain("one line to the user first"));
    }

    [Test]
    public void Puls_BriefsTheSpecialist_WithWhatTheUserWants_AndHowTheySound()
    {
        // F28 step 1 (Louis: "deduce the mood from the typing style and pass it to the specialist"): the task is a note
        // in a fixed shape, so the specialist, the card and the check can all read it; the mood is a read of the style.
        string agents = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice-af.md")!;

        Assert.That(agents, Does.Contain("## Briefing the specialist"));
        foreach (string label in new[] { "**Vir:**", "**Wil hê:**", "**Klink:**", "**Waar:**", "**Reeds gesê:**", "**For:**", "**Wants:**", "**Sounds:**", "**Where:**", "**Already said:**" })
            Assert.That(agents, Does.Contain(label), "the note's labels, in both languages");
        Assert.That(agents, Does.Contain("a guess from the typing").And.Contain("never a diagnosis"), "the guard rails");
        Assert.That(agents, Does.Contain("neutraal"), "when the typing says nothing");
        Assert.That(router, Does.Contain("**Klink:**"), "the one choosing is told the shape too");
    }

    [Test]
    public void TheMood_CanBeSwitchedOff_ForATurn()
    {
        // Louis: "an option for it, on or off during a session". The system prompt can't change mid-chat (FX4), so the
        // switch is a marker line the API puts before the user's text; the prompt says what it means.
        string agents = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice-af.md")!;

        Assert.That(router, Does.Contain("[stemming: af]"), "the marker, as the API writes it");
        Assert.That(agents, Does.Contain("[stemming: af]").And.Contain("no `Klink`"), "and what the note does without it");
    }

    [TestCase("puls-researcher")]
    [TestCase("puls-reader")]
    [TestCase("puls-historian")]
    [TestCase("puls-analyst")]
    [TestCase("puls-specialist")]
    [TestCase("puls-cook")]
    public void EverySpecialist_MatchesTheNotesTone_AndKeepsItsShape(string role)
    {
        string text = SkillFileSource.ReadEmbedded($"{Personalities.SpecialistsFolder}/{role}.md")!;

        Assert.That(text, Does.Contain("how the user sounds").And.Contain("keep your report's shape"));
    }

    [Test]
    public void OnlyTheCook_IsARoleToDelegate_UntilTheOthersPassTheirCheck()
    {
        // F29 step 3 (Louis: "disable all specialists, Puls boots with web, except the cook"): the prompts match the gate.
        // The web is Puls's own work; the other rows say "not yet", and a not-yet request is never handed over.
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice-af.md")!;
        string agents = SkillFileSource.ReadEmbedded("toolsets/agents.md")!;

        string[] routerRows = router.Split('\n').Where(r => r.StartsWith('|')).ToArray();
        Assert.That(routerRows.Single(r => r.Contains("The world:")), Does.Contain("you, with `web`").And.Not.Contain("puls-researcher"));
        Assert.That(routerRows.Where(r => r.Contains("`puls-")), Has.Exactly(1).Items.And.All.Contains("`puls-cook`"), "one role left in the table");
        Assert.That(routerRows.Count(r => r.Contains("*not yet*")), Is.GreaterThanOrEqualTo(6), "files, git, code, dotnet, paymo, devops, skills");
        Assert.That(router, Does.Contain("isn't available yet"), "what to say instead of delegating");

        string[] agentRows = agents.Split('\n').Where(r => r.StartsWith('|') && r.Contains("`puls-")).ToArray();
        Assert.That(agentRows, Has.Exactly(1).Items.And.All.Contains("`puls-cook`"), "the roles table keeps the cook");
        Assert.That(agents, Does.Contain("## Waiting for validation").And.Contain("PERSONALITIES_ENABLED"));
    }

    [TestCase("agent/puls/voice-af.md")]
    [TestCase("toolsets/agents.md")]
    public void AMealToCook_GoesToTheCook_WithWeb(string router)
    {
        // Puls routes by its tables (default.md says which toolset and role; the agents guide which role per toolset).
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
