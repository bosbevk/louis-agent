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
        // The cook check: Puls scaled the list itself in half the follow-ups, reading the rule as optional. Firmer now.
        Assert.That(router, Does.Contain("even when you could answer it yourself"));
        Assert.That(agents, Does.Contain("Continue, don't answer it yourself"));
    }

    [Test]
    public void EveryCookingQuestion_GoesToTheCook_WhichSaysWhereToBuy()
    {
        // Louis's manual test: "ek wil bebotie maak, wat het ek nodig" was answered from memory, no prices, no shop.
        // Cooking is the cook's even when Puls knows the recipe, and the cook names the branches near the user.
        string router = SkillFileSource.ReadEmbedded("default.md")!;
        string agents = SkillFileSource.ReadEmbedded("agents-skills.md")!;
        string cook = SkillFileSource.ReadEmbedded(Personalities.Folder + "/puls-cook.md")!;

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
        string cook = SkillFileSource.ReadEmbedded(Personalities.Folder + "/puls-cook.md")!;

        Assert.That(cook, Does.Contain("Puls hands your report to the user as it is"));
        Assert.That(cook, Does.Not.Contain("Puls will put it in its own voice"), "the line every other role has invites a rewrite");
        // Louis: one table, not the recipe's ingredients and then the list again.
        // Louis: two shops' prices per ingredient, and the totals merged into one table that compares them.
        Assert.That(cook, Does.Contain("| Ingredient | For the recipe | To buy | Checkers | Shoprite | Best price |"), "the shopping list's fixed columns");
        Assert.That(cook, Does.Contain("| **Total, priced rows** | | | **R142,97** | **R137,48** | **R131,97** |"), "the totals are the list's last rows, side by side, the cheapest basket last");
        // Louis: an unpriced ingredient is an ordinary row that says unknown; no row counting or naming them.
        Assert.That(cook, Does.Contain("| Lemons | 2 | 2 | [R12,99](https://www.checkers.co.za/…) | unknown | R12,99 Checkers |"));
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
        string agents = SkillFileSource.ReadEmbedded("agents-skills.md")!;
        string router = SkillFileSource.ReadEmbedded("default.md")!;

        Assert.That(agents, Does.Contain("## Relaying a report"));
        Assert.That(agents, Does.Contain("as the specialist wrote it").And.Contain("every table row"), "whole, tables included");
        Assert.That(router, Does.Contain("as the cook wrote it"), "the one choosing is told too");
    }

    [Test]
    public void Puls_HandsOffLikeAPerson_AndIntroducesTheReport()
    {
        // Louis: "say something like I'm handing this to my specialist, back shortly" before the call, and "my cook
        // just came back with this" above the report. The voice file said the opposite: never forward a report as is.
        string voice = SkillFileSource.ReadEmbedded(Personalities.Folder + "/puls.md")!;
        string agents = SkillFileSource.ReadEmbedded("agents-skills.md")!;
        string router = SkillFileSource.ReadEmbedded("default.md")!;

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
        string agents = SkillFileSource.ReadEmbedded("agents-skills.md")!;
        string router = SkillFileSource.ReadEmbedded("default.md")!;

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
        string agents = SkillFileSource.ReadEmbedded("agents-skills.md")!;
        string router = SkillFileSource.ReadEmbedded("default.md")!;

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
        string text = SkillFileSource.ReadEmbedded($"{Personalities.Folder}/{role}.md")!;

        Assert.That(text, Does.Contain("how the user sounds").And.Contain("keep your report's shape"));
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
