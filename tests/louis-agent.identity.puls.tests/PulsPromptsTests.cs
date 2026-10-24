using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Config;
using LouisAgent.Core.Hosting;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Identities.Puls;

namespace LouisAgent.Identities.Puls.Tests;

// CL15 step 3: Puls's content, tested with Puls. These tests came from core's RoleCatalogTests and MinionRolesTests
// with the prompts they read (F21, F25, F26, F28, F29, F30, F33, F34, FX13, FX17): each still reads the same file,
// now from the identity's own prompts, registered by PulsPrompts as the Puls stack registers them. Puls's briefing,
// its roles table and its relay lines read from agent/puls/voice.md, where the cut moved them out of the shared guide.
public class PulsPromptsTests
{
    private const string Modes = "louis-agent/voice, louis-agent/voice-orchestrator, puls/voice-af, puls/voice-en";

    private static string Read(string path) => SkillFileSource.ReadEmbedded(path) ?? throw new FileNotFoundException(path);

    [Test]
    public void PulsPrompts_ComeFromItsIdentity_NotFromCore()
    {
        // The red line of the move: the identity's own provider has the cook, and core's prompts don't.
        Assert.That(new PulsIdentity().Prompts.GetFileInfo("specialists/puls-cook.md").Exists, Is.True);
        Assert.That(SkillFileSource.CoreFiles, Has.None.Contains("puls"));
        Assert.That(Read("specialists/puls-cook.md"), Does.Contain("Local cook"), "read through the one reader");
    }

    [Test]
    public void PulsAndItsSpecialists_AreListed_WithPulsRegistered()
    {
        Assert.That(RoleCatalog.Available(RoleLevel.Agent), Is.EqualTo(Modes.Split(", ")));
        Assert.That(RoleCatalog.Available(RoleLevel.Specialist), Is.EqualTo(new[] { "puls-analyst", "puls-cook", "puls-historian", "puls-reader", "puls-researcher" }));
        Assert.That(RoleCatalog.Available(RoleLevel.Minion), Is.EqualTo(new[] { "puls-minion-web-price-lookup" }));
        Assert.That(SkillLoader.Load(new AgentOptions(), [], "puls/voice-af").Documentation, Does.Contain("# Puls - Stem"));
        Assert.That(SkillLoader.Load(new AgentOptions(), [], "puls-cook", RoleLevel.Specialist).Documentation, Does.Contain("Never invent a price"));
        Assert.That(RoleCatalog.ToolsetsOf("puls-cook", RoleLevel.Specialist), Is.EqualTo(new[] { "shops?" }), "F33: the cook's header");
    }

    [TestCase("af", "# Puls - Stem")]
    [TestCase("en", "# Puls - Voice")]
    public void APulsLanguage_LoadsItsVoice_ThenPulsShared(string code, string voice)
    {
        string docs = SkillLoader.Load(new AgentOptions(), [], $"puls/voice-{code}").Documentation;

        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal), Is.GreaterThan(0));
        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal), Is.LessThan(docs.IndexOf("# Puls - Routing", StringComparison.Ordinal)));
        Assert.That(Read("agent/puls/voice.md"), Does.StartWith("# Puls - Routing"), "Puls's shared part");
        Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], "puls/voice"), "the shared part is no mode of its own");
    }

    [Test]
    public void ThePulsLanguages_HaveTheSameSections()
    {
        static string[] Sections(string text) => text.Split('\n').Where(l => l.StartsWith("## ")).ToArray();
        string af = Read("agent/puls/voice-af.md"), en = Read("agent/puls/voice-en.md");

        Assert.That(Sections(en), Has.Length.EqualTo(Sections(af).Length));
        Assert.That(en.Split('\n').Count(l => l.StartsWith("- ")), Is.EqualTo(af.Split('\n').Count(l => l.StartsWith("- "))), "the same points");
    }

    [TestCase("agent/puls/voice-en.md", "# Puls - Voice")]
    [TestCase("agent/puls/voice-af.md", "# Puls - Stem")]
    public void AVoicesHeading_SaysVoice(string file, string heading) => Assert.That(Read(file), Does.StartWith(heading));

    [Test]
    public void Puls_RoutesOnlyWhatItDoes_AndCarriesItsOwnProtocol()
    {
        // CL9 step 3: a product's routing holds its own rows. CL15 step 3: and its own briefing, roles and relay lines.
        string puls = Read("agent/puls/voice.md");

        Assert.That(puls, Does.Contain("`web`").And.Contain("`specialists`").And.Contain("`puls-cook`").And.Contain("**Vir:**"));
        // Its routing table (the rows) names no coding toolset; the analyst's counting rule, moved here, may name files and python.
        string rows = string.Join("\n", puls.Split('\n').Where(r => r.StartsWith('|')));
        foreach (string coding in new[] { "`files`", "`git`", "`dotnet`", "`python`", "`paymo`", "`devops`", "`skills`" })
            Assert.That(rows, Does.Not.Contain(coding), $"Puls doesn't route {coding}");
        Assert.That(puls, Does.Contain("isn't available here yet"), "and still says so in one line when asked");
        Assert.That(puls, Does.Contain("## Briefing the specialist").And.Contain("## Relaying a report"), "the protocol, moved from the shared guide");
    }

    [Test]
    public void AMinionsPrompt_IsThePrinciples_TheBase_ThenItsRole()
    {
        string docs = SkillLoader.Load(new AgentOptions(), [], "puls-minion-web-price-lookup", RoleLevel.Minion).Documentation;

        int principles = docs.IndexOf("# Principles", StringComparison.Ordinal);
        int @base = docs.IndexOf("You are a **minion**", StringComparison.Ordinal);
        int role = docs.IndexOf("# Puls minion: Web price lookup", StringComparison.Ordinal);
        Assert.That(principles, Is.GreaterThanOrEqualTo(0));
        Assert.That(@base, Is.GreaterThan(principles), "the base after the principles");
        Assert.That(role, Is.GreaterThan(@base), "the role after the base");
        Assert.That(RoleCatalog.MinionBaseOf("puls-minion-web-price-lookup"), Is.EqualTo("puls-minion"));
    }

    [Test]
    public void TheCook_NamesItsMinion_WithWhatToHandIt()
    {
        // CL15 step 3: Puls's minion table and its Malmesbury example moved out of the shared minions guide into the cook.
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("`puls-minion-web-price-lookup`").And.Contain("`WebSearch`").And.Contain("Malmesbury"));
    }

    [Test]
    public void CountingGoesToTheAnalyst_AndTheReaderHandsItBack()
    {
        // FX13: a counting task sent to a files-only reader read every test file into a 298k-token prompt ($0.278).
        string router = Read("agent/puls/voice.md");
        string reader = Read("specialists/puls-reader.md");

        Assert.That(router, Does.Contain("Counting is the analyst's, never the reader's"));
        Assert.That(reader, Does.Contain("Don't count by reading").And.Contain("needs the analyst"));
    }

    [TestCase("puls-researcher")]
    [TestCase("puls-reader")]
    [TestCase("puls-historian")]
    [TestCase("puls-analyst")]
    [TestCase("puls-cook")]
    public void EverySpecialist_IsToldWhatItsToolCallsCost_AndMatchesTheNotesTone(string role)
    {
        // FX13: the reader that read 74 files never saw the router; the rule has to be in the role it runs with.
        string text = Read($"specialists/{role}.md");

        Assert.That(text, Does.Contain("Use the fewest tool calls that answer the task").And.Contain("paid for again"));
        Assert.That(text, Does.Contain("how the user sounds").And.Contain("keep your report's shape"));
    }

    [Test]
    public void ThePromptsTeach_StartSubSessionsRole()
    {
        // CL14 step 2: the model's word is role, in the tool's schema and in the prompts that teach it.
        string router = Read("agent/puls/voice.md");

        Assert.That(router, Does.Contain("StartSubSession(toolsets, message, role)").And.Not.Contain("personality"));
        Assert.That(router, Does.Contain("Pass `role`").And.Contain("ROLES_ENABLED"));
    }

    [Test]
    public void TheCook_AnswersForWhereTheUserIs_AndNeverInventsAPrice()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("Where the user is").And.Contain("Never invent a price"));
        Assert.That(cook, Does.Contain("the product's name as the shop sells it"));
        Assert.That(cook, Does.Contain("every ingredient named in the task's language"));
        Assert.That(cook, Does.Contain("never translated"));
        Assert.That(cook, Does.Contain("What isn't bought is never a row"));
        Assert.That(cook, Does.Contain("*Uit die voorraadkas: kookolie, sout en peper.*"));
        Assert.That(cook, Does.Contain("![KOO Chakalaka](https://catalog.sixty60.co.za/…) KOO Chakalaka Mild 410 g"));
        Assert.That(cook, Does.Contain("only an image a shop tool gave"));
        Assert.That(cook, Does.Contain("the smallest pack that covers what the recipe needs"));
        Assert.That(cook, Does.Contain("up by name in their town").And.Contain("q=Checkers+Malmesbury"));
        Assert.That(cook, Does.Contain("| Chicken thighs ").And.Contain("| 600 g ").And.Contain("Farmer's Choice Chicken Thighs 1–1,2 kg"));
    }

    [Test]
    public void AFollowUp_GoesToTheOpenSpecialist()
    {
        // F26 step 4: the prompt says when to continue and when to start over; the cook check showed the rule had to be firm.
        string router = Read("agent/puls/voice.md");

        Assert.That(router, Does.Contain("ContinueSubSession"));
        Assert.That(router, Does.Contain("even when you could answer it yourself"));
    }

    [Test]
    public void EveryCookingQuestion_GoesToTheCook_WhichSaysWhereToBuy()
    {
        string router = Read("agent/puls/voice.md");
        string cook = Read("specialists/puls-cook.md");

        Assert.That(router, Does.Contain("every cooking question").And.Contain("never from memory"));
        Assert.That(cook, Does.Contain("Where to buy").And.Contain("branches"));
        Assert.That(cook, Does.Contain("nominatim.openstreetmap.org/search").And.Contain("never fetch it, the link is for the user"));
        Assert.That(cook, Does.Contain("Geprys: {n} van {m} bestanddele"));
    }

    [Test]
    public void TheCooksLabels_AreInTheTasksLanguage()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("`Dien {n} · {tyd}`").And.Contain("`Serves {n} · {time}`"), "the recipe line");
        Assert.That(cook, Does.Contain("`Geprys: {n} van {m} bestanddele`").And.Contain("`Priced: {n} of {m} ingredients`"), "the tally");
        Assert.That(cook, Does.Not.Contain("One line, `Serves {n} · {total time}`"), "not English alone");
        Assert.That(cook, Does.Contain("in the task's language, never the other"));
    }

    [Test]
    public void ACountOfPacks_GoesAfterTheName_SoThePhotoStaysFirst()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("a count of packs goes after the name and pack, never before the photo"));
        Assert.That(cook, Does.Contain("Miami Italian Style Diced Tomatoes 410 g × 2`"), "the example");
    }

    [Test]
    public void TheCook_ReadsTheRecipeFirst_ThenDealsPickTheProducts_InPacksTheRecipeNeeds_ForAsManyPeopleAsPulsAsked()
    {
        // 2026-10-24 (Louis): "deals first, for every meal" went; the recipe, from a real page, comes first, and the deals
        // on its main ingredients pick the products.
        string cook = Read("specialists/puls-cook.md");
        string shops = Read("toolsets/shops.md");
        string router = Read("agent/puls/voice.md");

        Assert.That(cook, Does.Contain("The recipe first, from a real page, before any product").And.Contain("Deals pick the products"));
        Assert.That(cook, Does.Not.Contain("when cost matters"), "not only for a budget");
        Assert.That(shops, Does.Contain("Look for deals first"));
        Assert.That(cook, Does.Contain("product that fits a row is that row"), "no second search for it");
        Assert.That(cook, Does.Contain("the smallest pack that covers what the recipe needs").And.Not.Contain("`chakalaka 410g`"));
        Assert.That(shops, Does.Contain("the size the recipe needs"));
        Assert.That(router, Does.Contain("**Vir hoeveel:**").And.Contain("**Serves:**"), "its own line in every meal's brief");
        Assert.That(router, Does.Contain("Vir hoeveel mense kook jy?"), "Puls asks first, never a guess");
        Assert.That(router, Does.Contain("ask first, both in one short question"));
        Assert.That(cook, Does.Contain("cook for exactly that many").And.Not.Contain("cook for one"));
    }

    [Test]
    public void TheCook_PricesAtTheUsersShop_AndEstimatesWhereThereAreNoOnlinePrices()
    {
        // 2026-10-24: the task's Winkel / Shop line names the shop; a shop without online prices, or `any`, gets estimates.
        string cook = Read("specialists/puls-cook.md");
        string router = Read("agent/puls/voice.md");

        Assert.That(cook, Does.StartWith("---\ntoolsets: shops?\n---").Or.StartWith("---\r\ntoolsets: shops?\r\n---"));
        Assert.That(cook, Does.Contain("One or multiple shops, the user's").And.Contain("*Winkel* / *Shop*"));
        Assert.That(cook, Does.Contain("no online prices: estimates, as for `any`"));
        Assert.That(cook, Does.Contain("`SearchSpecials`").And.Contain("`SearchPromotions`"));
        Assert.That(cook, Does.Contain("with `area` = the user's town in every call"));
        Assert.That(cook, Does.Contain("every product you name, in any table"));
        Assert.That(cook, Does.Contain("the photo always first in the cell, then the product's name and pack"));
        Assert.That(router, Does.Contain("elke produk met sy foto").And.Contain("every product with its photo"));
        Assert.That(cook, Does.Contain("the link in the tool's table"), "its source");
        Assert.That(cook, Does.Not.Contain("pick two at random").And.Not.Contain("before any `WebSearch`"), "no shop without a tool");
        Assert.That(cook, Does.Not.Contain("Which shops' pages give a price").And.Not.Contain("`pnp.co.za`").And.Not.Contain("soekindeks"), "no prices from the web");
    }

    [Test]
    public void AShopWithoutOnlinePrices_GetsEstimates_ThatSaySo()
    {
        // 2026-10-24: instead of explaining why a shop can't be priced, the cook estimates and says so, in the task's language.
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("names a shop with no online prices"));
        Assert.That(cook, Does.Contain("written `± R65` with no link"));
        Assert.That(cook, Does.Contain("*Prices are estimates, not checked at a shop.*"));
        Assert.That(cook, Does.Contain("*Pryse is skattings, nie by 'n winkel nagegaan nie.*"));
        Assert.That(cook, Does.Not.Contain("public API"));
    }

    [Test]
    public void TheCook_WritesItsReportInOneShape_ThatPulsHandsOn()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("Puls hands your report to the user as it is"));
        Assert.That(cook, Does.Not.Contain("Puls will put it in its own voice"));
        Assert.That(cook, Does.Contain("| Ingredient ").And.Contain("| For the recipe ").And.Contain("| To buy ").And.Contain("| {shop} "));
        Assert.That(cook, Does.Contain("One or multiple shops, one price column per shop"));
        Assert.That(cook, Does.Contain("price at the shop's nearest store anyway"), "a town without the branch");
        Assert.That(cook, Does.Contain("| **Total, priced rows/per shop** |").And.Contain("| **R132,98**"));
        Assert.That(cook, Does.Contain("[R24,99](https://www.checkers.co.za/…) · BUY 2 FOR R45"));
        Assert.That(cook, Does.Contain("| Fresh coriander ").And.Contain("| Fresh coriander 20 g ").And.Contain("| -  "));
        Assert.That(cook, Does.Contain("a price you couldn't find is a `-` in its cell"));
        Assert.That(cook, Does.Not.Contain("**Unpriced"));
        Assert.That(cook, Does.Not.Contain("### Total"));
        Assert.That(cook, Does.Contain("never a price without its link"));
        Assert.That(cook, Does.Contain("don't list them here too"));
        Assert.That(cook, Does.Contain("| Shop | Branch | Street or mall | Website |"));
        Assert.That(cook, Does.Contain("the branch's own page on the shop's site").And.Contain("else the shop's home page"));
        Assert.That(cook, Does.Contain("| What | Why |"));
        int dish = cook.IndexOf("### The dish", StringComparison.Ordinal), list = cook.IndexOf("### Shopping list", StringComparison.Ordinal),
            buy = cook.IndexOf("### Where to buy", StringComparison.Ordinal), unsure = cook.IndexOf("### Unsure", StringComparison.Ordinal);
        Assert.That(dish, Is.GreaterThan(-1).And.LessThan(list));
        Assert.That(list, Is.LessThan(buy));
        Assert.That(buy, Is.LessThan(unsure));
    }

    [Test]
    public void Puls_RelaysTheCooksReport_HandsOffLikeAPerson_AndIntroducesIt()
    {
        string voice = Read("agent/puls/voice-af.md");
        string router = Read("agent/puls/voice.md");

        Assert.That(router, Does.Contain("as the cook wrote it"), "the one choosing is told to hand it on whole");
        Assert.That(router, Does.Contain("Ek gee dit gou vir my kok").And.Contain("My kok is terug"), "the relay lines, in Puls's words");
        Assert.That(router, Does.Contain("puls-cook): 4 requests"), "the footer example names its own cook");
        Assert.That(voice, Does.Not.Contain("Moet nooit 'n spesialis se verslag net so aanstuur nie"));
        Assert.That(voice, Does.Contain("my kok").And.Contain("nou-nou terug"), "the hand-off line, in Puls's voice");
        Assert.That(router, Does.Contain("one line to the user first"));
    }

    [Test]
    public void Puls_BriefsTheSpecialist_WithWhatTheUserWants_AndHowTheySound()
    {
        // F28 step 1: the task is a note in a fixed shape; the mood is a read of the typing. CL15: in Puls's own routing.
        string router = Read("agent/puls/voice.md");

        foreach (string label in new[] { "**Vir:**", "**Wil hê:**", "**Vir hoeveel:**", "**Klink:**", "**Waar:**", "**Reeds gesê:**", "**For:**", "**Wants:**", "**Serves:**", "**Sounds:**", "**Where:**", "**Already said:**" })
            Assert.That(router, Does.Contain(label), "the note's labels, in both languages");
        Assert.That(router, Does.Contain("a guess from the typing").And.Contain("never a diagnosis"), "the guard rails");
        Assert.That(router, Does.Contain("neutraal"), "when the typing says nothing");
        Assert.That(router, Does.Contain("[stemming: af]").And.Contain("no `Klink`"), "the mood can be switched off for a turn");
    }

    [Test]
    public void OnlyTheCook_IsARoleToDelegate_UntilTheOthersPassTheirCheck()
    {
        // F29 step 3 (Louis: "disable all specialists, Puls boots with web, except the cook"): the prompts match the gate.
        string router = Read("agent/puls/voice.md");

        string[] rows = router.Split('\n').Where(r => r.StartsWith('|')).ToArray();
        Assert.That(rows.Single(r => r.Contains("The world:")), Does.Contain("you, with `web`").And.Not.Contain("puls-researcher"));
        Assert.That(rows.Where(r => r.Contains("`puls-")), Has.Exactly(3).Items.And.All.Contains("`puls-cook`"), "the routing row and the two roles rows (cooking, shopping)");
        Assert.That(rows.Single(r => r.Contains("`puls-cook`") && r.Contains("`web`") && r.Contains("Cooking")), Is.Not.Null, "a meal goes to the cook with web");
        Assert.That(router, Does.Contain("never hand anything else to a specialist"));
        Assert.That(router, Does.Contain("isn't available yet"), "what to say instead of delegating");
        Assert.That(router, Does.Contain("### Waiting for validation").And.Contain("ROLES_ENABLED"));
    }
}
