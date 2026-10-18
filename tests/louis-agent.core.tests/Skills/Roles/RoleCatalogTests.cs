using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;

namespace LouisAgent.Core.Tests.Skills.Roles;

// F21 step 1: a session names its role (its personality before CL13); the prompt starts with that one instead of personality.md, and a name
// never reaches outside its level's folder (CL8: Skills/agent/, Skills/specialists/).
public class RoleCatalogTests
{
    private string _skills = null!;

    [SetUp]
    public void SetUp()
    {
        _skills = Path.Combine(Path.GetTempPath(), "puls-personalities-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_skills, RoleCatalog.AgentFolder));
        File.WriteAllText(Path.Combine(_skills, RoleCatalog.AgentFolder, "test-voice.md"), "# Test Voice\n\nKort en kragtig.\n");
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_skills, recursive: true);

    private AgentOptions Options() => new() { SkillsDirectory = _skills };

    [Test]
    public void NoName_IsTheDefaultMode()
    {
        string docs = AgentHost.LoadSkills(Options(), [], role: null).Documentation;

        Assert.That(docs, Does.Contain("Louis's Minion"), "the default voice, louis-agent/voice (personality.md before CL9)");
        Assert.That(docs, Does.Not.Contain("# Puls - Stem"));
        Assert.That(docs, Does.Contain("# Louis Agent - Default Instructions"), "and its router");
    }

    [Test]
    public void AMode_IsOneFile_ItsVoiceThenItsRouting()
    {
        // CL9: a level-0 session loads one file; its voice frames everything after it, its routing follows.
        string docs = AgentHost.LoadSkills(new AgentOptions(), [], "puls/voice-af").Documentation;

        Assert.That(RoleCatalog.DefaultVoice, Is.EqualTo("louis-agent/voice"));
        Assert.That(docs.IndexOf("# Puls - Stem", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Puls - Routing", StringComparison.Ordinal)));
        Assert.That(SkillFileSource.ReadEmbedded("agent/puls/voice.md"), Does.StartWith("# Puls - Routing"), "Puls's shared part");
    }

    [Test]
    public void ANamedRole_ReplacesTheDefault_AndComesFirst()
    {
        string docs = AgentHost.LoadSkills(Options(), [], "puls/voice-af").Documentation;

        Assert.That(docs, Does.Contain("# Puls - Stem"));
        Assert.That(docs, Does.Not.Contain("Louis's Minion"));
        Assert.That(docs.IndexOf("# Puls - Stem", StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Puls - Routing", StringComparison.Ordinal)), "who the agent is, before its routing");
    }

    [TestCase("../personality")]
    [TestCase("Puls")]
    [TestCase("a/b/c")]
    [TestCase("voice.md")]
    [TestCase("-voice")]
    public void ANameThatIsntValid_IsRefused(string name)
    {
        var ex = Assert.Throws<RoleException>(() => AgentHost.LoadSkills(Options(), [], name));
        Assert.That(ex!.Message, Does.Contain("lowercase letters, digits and dashes"));
    }

    [Test]
    public void AnUnknownName_IsRefused_ListingTheAvailable()
    {
        var ex = Assert.Throws<RoleException>(() => AgentHost.LoadSkills(Options(), [], "nobody"));
        Assert.That(ex!.Message, Does.Contain("Unknown role 'nobody'").And.EndWith("Available: louis-agent/voice, louis-agent/voice-orchestrator, puls/voice-af, puls/voice-en."));
    }

    [Test]
    public void AVoiceOnDisk_IsNeitherOfferedNorLoaded()
    {
        // CL8 step 4: roles are built in; the skills folder holds only what the agent writes.
        Assert.That(RoleCatalog.Available(RoleLevel.Agent), Does.Not.Contain("test-voice"));
        Assert.Throws<RoleException>(() => AgentHost.LoadSkills(Options(), [], "test-voice"));
    }

    [Test]
    public void PulsAndItsSpecialists_AreBuiltIn()
    {
        // F21 step 4: embedded, so a container without Skills/ still has them; a file on disk overrides.
        Assert.That(RoleCatalog.Available(RoleLevel.Agent), Is.EqualTo(new[] { "louis-agent/voice", "louis-agent/voice-orchestrator", "puls/voice-af", "puls/voice-en" }));
        Assert.That(RoleCatalog.Available(RoleLevel.Specialist), Is.SupersetOf(new[]
            { "puls-researcher", "puls-reader", "puls-historian", "puls-analyst", "puls-cook" }));
        Assert.That(RoleCatalog.Available(RoleLevel.Specialist), Does.Not.Contain("puls-specialist"), "CL10: no function of its own");
        Assert.That(AgentHost.LoadSkills(new AgentOptions(), [], "puls/voice-af").Documentation, Does.Contain("# Puls - Stem"));
        Assert.That(AgentHost.LoadSkills(new AgentOptions(), [], "puls-cook", RoleLevel.Specialist).Documentation,
            Does.Contain("Never invent a price"));
    }

    [Test]
    public void EachProduct_RoutesOnlyWhatItDoes()
    {
        // CL9 step 3: a product's routing holds its own rows, so neither pays for the other's in every request.
        string puls = SkillFileSource.ReadEmbedded("agent/puls/voice.md")!;
        string louis = RoleCatalog.ModeParts(SkillFileSource.ReadEmbedded("agent/louis-agent/voice.md")!).Routing;

        Assert.That(puls, Does.Contain("`web`").And.Contain("`specialists`").And.Contain("`puls-cook`").And.Contain("**Vir:**"));
        foreach (string coding in new[] { "`files`", "`git`", "`dotnet`", "`python`", "`paymo`", "`devops`", "`skills`" })
            Assert.That(puls, Does.Not.Contain(coding), $"Puls doesn't route {coding}");
        Assert.That(puls, Does.Contain("isn't available here yet"), "and still says so in one line when asked");
        Assert.That(louis, Does.Not.Contain("puls-cook").And.Not.Contain("**Vir:**").And.Not.Contain("cook"), "no cook, no Afrikaans briefing");
    }

    [TestCase("af", "# Puls - Stem")]
    [TestCase("en", "# Puls - Voice")]
    public void APulsLanguage_LoadsItsVoice_ThenPulsShared(string code, string voice)
    {
        // CL9 step 6 (Louis: "a puls/voice.md for shared things"): the language file says how Puls talks, voice.md routes.
        string docs = AgentHost.LoadSkills(new AgentOptions(), [], $"puls/voice-{code}").Documentation;

        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal), Is.GreaterThan(0));
        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal), Is.LessThan(docs.IndexOf("# Puls - Routing", StringComparison.Ordinal)));
        Assert.That(RoleCatalog.LanguageOf($"puls/voice-{code}"), Is.EqualTo(code));
    }

    [Test]
    public void PulsVoice_IsShared_NotAModeOnItsOwn()
    {
        Assert.That(RoleCatalog.Available(RoleLevel.Agent), Is.EqualTo(new[] { "louis-agent/voice", "louis-agent/voice-orchestrator", "puls/voice-af", "puls/voice-en" }));
        Assert.Throws<RoleException>(() => AgentHost.LoadSkills(new AgentOptions(), [], "puls/voice"));
        Assert.That(RoleCatalog.LanguageOf("louis-agent/voice"), Is.Null, "the coding agent's mode is whole");
    }

    [Test]
    public void ThePulsLanguages_HaveTheSameSections()
    {
        // Two languages of one voice drift apart unless something holds them together: the same sections, in order.
        static string[] Sections(string text) => text.Split('\n').Where(l => l.StartsWith("## ")).ToArray();
        string af = SkillFileSource.ReadEmbedded("agent/puls/voice-af.md")!, en = SkillFileSource.ReadEmbedded("agent/puls/voice-en.md")!;

        Assert.That(Sections(en), Has.Length.EqualTo(Sections(af).Length));
        Assert.That(en.Split('\n').Count(l => l.StartsWith("- ")), Is.EqualTo(af.Split('\n').Count(l => l.StartsWith("- "))), "the same points");
    }

    [TestCase("functions/code-review")]
    [TestCase("puls")]
    [TestCase("default")]
    public void OnlyAMode_IsANameAtLevelZero(string name)
    {
        // CL9: a mode is agent/{product}/{mode}.md; a function skill, a product folder or an old name isn't one.
        var ex = Assert.Throws<RoleException>(() => AgentHost.LoadSkills(new AgentOptions(), [], name));

        Assert.That(ex!.Message, Does.EndWith("Available: louis-agent/voice, louis-agent/voice-orchestrator, puls/voice-af, puls/voice-en."));
    }

    [Test]
    public void EachLevel_ListsOnlyItsOwnRoles()
    {
        // CL8: the folders follow the levels; F30 step 4 fills minions/ (level 2) with its first role.
        Assert.That(RoleCatalog.Available(RoleLevel.Specialist), Does.Not.Contain("puls/voice-af").And.Not.Contain("puls-minion-web-price-lookup"));
        Assert.That(RoleCatalog.Available(RoleLevel.Minion), Is.EqualTo(new[] { "puls-minion-web-price-lookup" }));
    }

    [Test]
    public void ASpecialistsRole_IsRefusedAtLevelZero_NamingTheVoicesThereAre()
    {
        // CL8: a role opens only at its own level, so Puls's chat can't take the cook's role as its voice.
        var ex = Assert.Throws<RoleException>(() => AgentHost.LoadSkills(new AgentOptions(), [], "puls-cook", RoleLevel.Agent));

        Assert.That(ex!.Message, Does.Contain("Unknown role 'puls-cook'").And.EndWith("Available: louis-agent/voice, louis-agent/voice-orchestrator, puls/voice-af, puls/voice-en."));
    }

    [Test]
    public void TheAgentsVoice_IsRefusedAtLevelOne_NamingTheRolesThereAre()
    {
        var ex = Assert.Throws<RoleException>(() => AgentHost.LoadSkills(new AgentOptions(), [], "puls/voice-af", RoleLevel.Specialist));

        Assert.That(ex!.Message, Does.Contain("Unknown role 'puls/voice-af'").And.Contain("puls-cook"));
    }

    [Test]
    public void OnlyLevelZero_HasTheRouter()
    {
        // The router picks toolsets and delegates; a specialist's toolsets were chosen for it (F21), and so are a minion's.
        Assert.That(AgentHost.LoadSkills(new AgentOptions(), [], level: RoleLevel.Agent).Documentation,
            Does.Contain("# Louis Agent - Default Instructions"));
        Assert.That(AgentHost.LoadSkills(new AgentOptions(), [], level: RoleLevel.Specialist).Documentation,
            Does.Not.Contain("# Louis Agent - Default Instructions"));
    }

    [Test]
    public void CountingGoesToTheAnalyst_AndTheReaderHandsItBack()
    {
        // FX13: a counting task sent to a files-only reader read every test file into a 298k-token prompt ($0.278).
        string router = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;
        string reader = SkillFileSource.ReadEmbedded(RoleCatalog.SpecialistsFolder + "/puls-reader.md")!;

        Assert.That(router, Does.Contain("Counting is the analyst's, never the reader's"));
        Assert.That(router, Does.Contain("Every sub-session costs money"), "the one choosing is told the cost too");
        Assert.That(reader, Does.Contain("Don't count by reading").And.Contain("needs the analyst"));
    }

    [TestCase("puls-researcher")]
    [TestCase("puls-reader")]
    [TestCase("puls-historian")]
    [TestCase("puls-analyst")]
    [TestCase("puls-cook")]
    public void EverySpecialist_IsToldWhatItsToolCallsCost(string role)
    {
        // FX13: the reader that read 74 files never saw the router; the rule has to be in the role it runs with.
        string text = SkillFileSource.ReadEmbedded($"{RoleCatalog.SpecialistsFolder}/{role}.md")!;

        Assert.That(text, Does.Contain("Use the fewest tool calls that answer the task").And.Contain("paid for again"));
    }

    [Test]
    public void ThePromptsTeach_StartSubSessionsRole()
    {
        // CL14 step 2: the model's word is role, in the tool's schema and in the prompts that teach it.
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice.md")!;
        string guide = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;

        Assert.That(router, Does.Contain("StartSubSession(toolsets, message, role)").And.Not.Contain("personality"));
        Assert.That(guide, Does.Contain("Pass `role`").And.Contain("ROLES_ENABLED").And.Not.Contain("personality").IgnoreCase);
    }

    [TestCase("agent/louis-agent/voice.md", "# Louis's Minion - Voice")]
    [TestCase("agent/puls/voice-en.md", "# Puls - Voice")]
    [TestCase("agent/puls/voice-af.md", "# Puls - Stem")]
    public void AVoicesHeading_SaysVoice(string file, string heading) =>
        Assert.That(SkillFileSource.ReadEmbedded(file)!, Does.StartWith(heading));

    [Test]
    public void TheCook_AnswersForWhereTheUserIs_AndNeverInventsAPrice()
    {
        // The local-specialist idea: one question gets a South African recipe, where to buy it and what it costs.
        string cook = SkillFileSource.ReadEmbedded(RoleCatalog.SpecialistsFolder + "/puls-cook.md")!;

        Assert.That(cook, Does.Contain("Where the user is").And.Contain("Never invent a price"));
        // PR #58's review: run fc7f8ecfdc87 priced a 1–1.2 kg pack of thighs at its price per kg.
        Assert.That(cook, Does.Contain("A price per kg is never a pack's price"));
        // Louis (PR #58): the list's To buy column showed "1 kg" and "6 pak" with no product; the example taught it.
        Assert.That(cook, Does.Contain("the product's name as the shop sells it"));
        // Louis, on an Afrikaans list with "Icing sugar": the ingredient in the task's language; then "the product should
        // display as it comes from checkers, we should not specifically translate".
        Assert.That(cook, Does.Contain("every ingredient named in the task's language"));
        Assert.That(cook, Does.Contain("never translated"));
        // Louis, on "Geprys: 10 van 11" with tap water a row, then "8 van 10" with oil and salt "uit die voorraadkas":
        // "it should now say 8 of 8". What isn't bought (tap water, pantry staples) is a line under the list, not a row.
        Assert.That(cook, Does.Contain("What isn't bought is never a row"));
        Assert.That(cook, Does.Contain("*Uit die voorraadkas: kookolie, sout en peper.*"));
        // Louis: "can we have a product image": the photo FindProducts gives, before the name; never another image.
        Assert.That(cook, Does.Contain("![KOO Chakalaka](https://catalog.sixty60.co.za/…) KOO Chakalaka Mild 410 g"));
        Assert.That(cook, Does.Contain("only an image a shop tool gave"));
        // Louis: the recipe needed under 1 kg of maize meal and the cook priced the 2.5 kg pack, with a 1 kg one on the shelf.
        Assert.That(cook, Does.Contain("the smallest pack that covers what the recipe needs"));
        // Louis: "there is a checkers in malmesbury so why is puls not picking it up". "supermarkets in Malmesbury" on
        // OpenStreetMap misses it (mapped on the N7, outside the town's boundary); a search by name finds it.
        Assert.That(cook, Does.Contain("look it up by name").And.Contain("q=Checkers+Malmesbury"));
        Assert.That(cook, Does.Contain("| Chicken thighs | 600 g | Farmer's Choice Chicken Thighs 1–1,2 kg |"));
    }

    [Test]
    public void AFollowUp_GoesToTheOpenSpecialist()
    {
        // F26 step 4: the tool exists from the first turn; the prompt says when to continue and when to start over.
        string agents = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice.md")!;

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
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice.md")!;
        string agents = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;
        string cook = SkillFileSource.ReadEmbedded(RoleCatalog.SpecialistsFolder + "/puls-cook.md")!;

        Assert.That(router, Does.Contain("every cooking question").And.Contain("never from memory"));
        Assert.That(agents, Does.Contain("every cooking question"));
        Assert.That(cook, Does.Contain("Where to buy").And.Contain("branches"));
        // Louis's second test: the shops' store finders and search pages are JavaScript and gave the cook nothing, while
        // OpenStreetMap's search returns the branches as JSON; the role names it, and says which pages not to fetch.
        Assert.That(cook, Does.Contain("nominatim.openstreetmap.org/search").And.Contain("Don't fetch"));
        // Louis: a rating, how many ingredients got a price. The cook states it in a fixed line the check reads.
        Assert.That(cook, Does.Contain("Geprys: {n} van {m} bestanddele"));
    }

    // Louis's chats of 2026-10-18 (R100, R75, R50 for Malmesbury): about 40 shop searches, one SearchSpecials, after
    // the list was priced. "the cook should probably search for deals or specials first". The rice was searched as
    // `long grain rice 1kg` for 300 g, so the 500 g pack never showed: "the size must agree with the recipe". A brief
    // with no number of people got a meal for two: "it also assumed 2 people, i meant 1", then "the cook must always
    // know how many people it is for", and, asked how: "Puls asks first".
    [Test]
    public void TheCook_LooksForDealsFirst_InPacksTheRecipeNeeds_ForAsManyPeopleAsPulsAsked()
    {
        string cook = SkillFileSource.ReadEmbedded("specialists/puls-cook.md")!;
        string shops = SkillFileSource.ReadEmbedded("toolsets/shops.md")!;
        string guide = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;

        Assert.That(cook, Does.Contain("Deals first, for every meal"));
        Assert.That(cook, Does.Not.Contain("when cost matters"), "not only for a budget");
        Assert.That(shops, Does.Contain("Look for deals first"));
        Assert.That(cook, Does.Contain("a deal product that fits a row is that row"), "no second search for it");

        Assert.That(cook, Does.Contain("the size the recipe needs").And.Not.Contain("`chakalaka 410g`"));
        Assert.That(shops, Does.Contain("the size the recipe needs"));

        Assert.That(guide, Does.Contain("**Vir hoeveel:**").And.Contain("**Serves:**"), "its own line in every meal's brief");
        Assert.That(guide, Does.Contain("Vir hoeveel mense kook jy?"), "Puls asks first, never a guess");
        Assert.That(SkillFileSource.ReadEmbedded("agent/puls/voice.md")!, Does.Contain("ask how many people first"));
        Assert.That(cook, Does.Contain("cook for exactly that many").And.Not.Contain("cook for one"));
    }

    [Test]
    public void TheCook_PricesOnlyShopsItHasAToolFor()
    {
        // F34 step 4 (Louis: "the cook may only use shops for who has tools for"; with none, the recipe with no prices).
        // F33's cook priced two shops picked at random, through WebSearch for those without a catalogue; with every
        // search engine refusing (the Malmesbury chat), that gave no price with a source. Now a price comes only from a
        // shop tool, linked to the product the table gives, a deal named beside it.
        string cook = SkillFileSource.ReadEmbedded(RoleCatalog.SpecialistsFolder + "/puls-cook.md")!;

        Assert.That(cook, Does.StartWith("---\ntoolsets: shops?\n---").Or.StartWith("---\r\ntoolsets: shops?\r\n---"));
        Assert.That(cook, Does.Contain("Price only at a shop your shop tools cover"));
        Assert.That(cook, Does.Contain("No shop near you has a catalogue here yet").And.Contain("Geen winkel naby jou het nog 'n katalogus hier nie"));
        Assert.That(cook, Does.Contain("`SearchSpecials`").And.Contain("`SearchPromotions`"));
        Assert.That(cook, Does.Contain("pass `area`: the user's town from the task"), "run dd992620f6cc: Pretoria only in the message");
        // Louis, on Puls's brief for frozen pizza specials (a table with no photos): "always send a product image with a
        // product". Puls asks for it in every brief about products; the cook shows it in every table, not only the list.
        Assert.That(cook, Does.Contain("every product you name, in any table"));
        // Louis: "die foto moet altyd eerste wees en dan produk desc": in the product's cell, the photo, then its name.
        Assert.That(cook, Does.Contain("the photo always first in the cell, then the product's name and pack"));
        string guide = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;
        Assert.That(guide, Does.Contain("elke produk met sy foto").And.Contain("every product with its photo"));
        Assert.That(cook, Does.Contain("the link the table gives"), "its source");
        Assert.That(cook, Does.Not.Contain("pick two at random").And.Not.Contain("before any `WebSearch`"), "no shop without a tool");
        Assert.That(cook, Does.Not.Contain("Which shops' pages give a price").And.Not.Contain("`pnp.co.za`").And.Not.Contain("soekindeks"),
            "no prices from the web");
    }

    [Test]
    public void AShopThePriceCantComeFrom_IsExplained_AndTheUserAskedToRaiseIt()
    {
        // Louis: "when a shop does not have a provider puls must reply to the user politely to take it up with the store
        // in order to get listed", "since they do not have an API": the reason in plain words, and the ask on the public.
        string cook = SkillFileSource.ReadEmbedded(RoleCatalog.SpecialistsFolder + "/puls-cook.md")!;

        Assert.That(cook, Does.Contain("Puls can show prices from shops that make them available online in a form it can read, such as Checkers."));
        Assert.That(cook, Does.Contain("the more customers ask, the sooner it happens"));
        Assert.That(cook, Does.Contain("Puls kan pryse wys van winkels wat dit aanlyn beskikbaar stel in 'n vorm wat dit kan lees, soos Checkers."));
        Assert.That(cook, Does.Contain("hoe meer klante vra, hoe gouer gebeur dit"));
        Assert.That(cook, Does.Not.Contain("public API"), "Checkers publishes no API as such: what's true is that its prices can be read");
    }

    [Test]
    public void TheCook_WritesItsReportInOneShape_ThatPulsHandsOn()
    {
        // Louis: "force Puls to output the data as the cook said it, and conform the cook's output". The report is
        // written for the user in a fixed shape (headings in order, the list and the branches as tables with fixed
        // columns, a price only with its source), so Puls can hand it on whole and the check can read it row by row.
        string cook = SkillFileSource.ReadEmbedded(RoleCatalog.SpecialistsFolder + "/puls-cook.md")!;

        Assert.That(cook, Does.Contain("Puls hands your report to the user as it is"));
        Assert.That(cook, Does.Not.Contain("Puls will put it in its own voice"), "the line every other role has invites a rewrite");
        // Louis: one table, not the recipe's ingredients and then the list again; a column per shop priced at (F34: only
        // shops with a tool, so Checkers today), and a best-price column only when there's more than one.
        Assert.That(cook, Does.Contain("| Ingredient | For the recipe | To buy | Checkers |"), "the shopping list's columns: a column per shop priced at");
        // The cheapest basket can exceed a shop's total that leaves rows out, so it's bounded by the completed column.
        Assert.That(cook, Does.Contain("the cheapest basket is never more than the completed"), "the best-price total the model got wrong");
        // Louis: "don't even use stores not in your location".
        Assert.That(cook, Does.Contain("never price at, name or total a shop the town doesn't have"));
        Assert.That(cook, Does.Contain("| **Total, priced rows** | | | **R132,98** |"), "the totals are the list's last rows");
        // F34: a deal is named beside the pack's price, as the shop words it.
        Assert.That(cook, Does.Contain("[R24,99](https://www.checkers.co.za/…) · BUY 2 FOR R45"));
        // Louis: an unpriced ingredient is an ordinary row; no row counting or naming them. Its cell is a `-` (review of
        // PR #55: "make unknown --> -"), so a column of gaps reads at a glance and the check's scorer counts the row's
        // linked price at another shop.
        Assert.That(cook, Does.Contain("| Fresh coriander | 20 g | Fresh coriander 20 g | - |"));
        Assert.That(cook, Does.Contain("a price you couldn't find is a `-` in its cell"));
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
        string agents = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice.md")!;

        Assert.That(agents, Does.Contain("## Relaying a report"));
        Assert.That(agents, Does.Contain("as the specialist wrote it").And.Contain("every table row"), "whole, tables included");
        Assert.That(router, Does.Contain("as the cook wrote it"), "the one choosing is told too");
        // Louis, on Puls's answer ending "[Sub-session sub-1 on puls (web; puls-cook): 4 requests, …]": "bug". "Every
        // line under a table" took in the tool's footer, the last line under the last table. The footer is Puls's alone.
        Assert.That(agents, Does.Contain("The footer the tool adds under the report"));
        Assert.That(agents, Does.Contain("never in your answer"));
    }

    [Test]
    public void Puls_HandsOffLikeAPerson_AndIntroducesTheReport()
    {
        // Louis: "say something like I'm handing this to my specialist, back shortly" before the call, and "my cook
        // just came back with this" above the report. The voice file said the opposite: never forward a report as is.
        string voice = SkillFileSource.ReadEmbedded(RoleCatalog.AgentFolder + "/puls/voice-af.md")!;
        string agents = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice.md")!;

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
        string agents = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice.md")!;

        Assert.That(agents, Does.Contain("## Briefing the specialist"));
        foreach (string label in new[] { "**Vir:**", "**Wil hê:**", "**Vir hoeveel:**", "**Klink:**", "**Waar:**", "**Reeds gesê:**", "**For:**", "**Wants:**", "**Serves:**", "**Sounds:**", "**Where:**", "**Already said:**" })
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
        string agents = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice.md")!;

        Assert.That(router, Does.Contain("[stemming: af]"), "the marker, as the API writes it");
        Assert.That(agents, Does.Contain("[stemming: af]").And.Contain("no `Klink`"), "and what the note does without it");
    }

    [TestCase("puls-researcher")]
    [TestCase("puls-reader")]
    [TestCase("puls-historian")]
    [TestCase("puls-analyst")]
    [TestCase("puls-cook")]
    public void EverySpecialist_MatchesTheNotesTone_AndKeepsItsShape(string role)
    {
        string text = SkillFileSource.ReadEmbedded($"{RoleCatalog.SpecialistsFolder}/{role}.md")!;

        Assert.That(text, Does.Contain("how the user sounds").And.Contain("keep your report's shape"));
    }

    [Test]
    public void OnlyTheCook_IsARoleToDelegate_UntilTheOthersPassTheirCheck()
    {
        // F29 step 3 (Louis: "disable all specialists, Puls boots with web, except the cook"): the prompts match the gate.
        // The web is Puls's own work; the other rows say "not yet", and a not-yet request is never handed over.
        string router = SkillFileSource.ReadEmbedded("agent/puls/voice.md")!;
        string agents = SkillFileSource.ReadEmbedded("toolsets/specialists.md")!;

        string[] routerRows = router.Split('\n').Where(r => r.StartsWith('|')).ToArray();
        Assert.That(routerRows.Single(r => r.Contains("The world:")), Does.Contain("you, with `web`").And.Not.Contain("puls-researcher"));
        Assert.That(routerRows.Where(r => r.Contains("`puls-")), Has.Exactly(1).Items.And.All.Contains("`puls-cook`"), "one role left in the table");
        // CL9 step 3: the not-yet rows (files, git, code, dotnet, paymo, devops, skills) became one sentence in Puls's routing.
        Assert.That(router, Does.Contain("never hand anything else to a specialist"));
        Assert.That(router, Does.Contain("isn't available yet"), "what to say instead of delegating");

        string[] agentRows = agents.Split('\n').Where(r => r.StartsWith('|') && r.Contains("`puls-")).ToArray();
        Assert.That(agentRows, Has.Exactly(1).Items.And.All.Contains("`puls-cook`"), "the roles table keeps the cook");
        Assert.That(agents, Does.Contain("## Waiting for validation").And.Contain("ROLES_ENABLED"));
    }

    [TestCase("agent/puls/voice.md")]
    [TestCase("toolsets/specialists.md")]
    public void AMealToCook_GoesToTheCook_WithWeb(string router)
    {
        // Puls routes by its tables (default.md says which toolset and role; the specialists guide which role per toolset).
        // Without a row in both, "what should I cook?" is just another web question and goes to the researcher.
        string[] rows = SkillFileSource.ReadEmbedded(router)!.Split('\n');
        string? row = rows.FirstOrDefault(r => r.StartsWith('|') && r.Contains("`puls-cook`"));

        Assert.That(row, Is.Not.Null, $"{router} has no table row for the cook");
        Assert.That(row, Does.Contain("`web`").And.Contain("cook").IgnoreCase);
    }

    [Test]
    public void ARole_IsNamed_NeverDerived()
    {
        // CL10: Puls names the cook; nothing picks a role from a sub-session's toolsets any more, and the guide says so.
        Assert.That(SkillFileSource.ReadEmbedded("specialists/roles.json"), Is.Null);
        Assert.That(SkillFileSource.ReadEmbedded("toolsets/specialists.md"), Does.Not.Contain("roles.json"));
    }

}
