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

    /// <summary>A prompt with its line breaks and indents as single spaces, for a phrase the file wraps.</summary>
    private static string Flat(string text) => System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");

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
        Assert.That(SkillLoader.Load(new AgentOptions(), [], "puls-cook", RoleLevel.Specialist).Documentation, Does.Contain("No prices are looked up"));
        Assert.That(RoleCatalog.ToolsetsOf("puls-cook", RoleLevel.Specialist), Is.Empty, "F18 step 5.7: the cook has no header now, it looks up no prices (the shops pack stays, off for the cook)");
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
    public void TheCook_NamesNoPriceLookupMinion_NowThatItLooksUpNoPrices()
    {
        // F18 step 5.7: the cook's minion table (CL15 step 3) handed one ingredient to a price-lookup minion; with no prices looked up it is gone from the cook.
        // The minion role stays, with its tests, for when a retailer says yes.
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Not.Contain("puls-minion-web-price-lookup").And.Not.Contain("## Minions"));
        Assert.That(RoleCatalog.Available(RoleLevel.Minion), Is.EqualTo(new[] { "puls-minion-web-price-lookup" }));
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
    public void TheCook_AnswersForWhereTheUserIs_AndQuotesNoPrice()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("Where the user is").And.Contain("they are never a quote"));
        Assert.That(cook, Does.Contain("every ingredient named in the task's language"));
        Assert.That(cook, Does.Contain("What isn't bought is never a row"));
        Assert.That(cook, Does.Contain("*Uit die voorraadkas: kookolie, sout, peper, suiker en speserye.*"));
        Assert.That(cook, Does.Contain("no brand, no photo"));
        Assert.That(cook, Does.Contain("up by name in their town").And.Contain("q=Pick+n+Pay+Malmesbury"));
        Assert.That(cook, Does.Contain("| Chicken thighs ").And.Contain("| 600 g ").And.Contain("chicken thighs 1 kg"));
    }

    [Test]
    public void TheCook_ShowsOneImageOfTheDish_FromTheRecipePageItRead()
    {
        // Louis, 2026-10-25: "i want at least one image to render from the recipe". FetchUrl and ReadRenderedPage both return images as `![alt](url)`.
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("one image of the dish").And.Contain("![Bobotie](https://…)"));
        Assert.That(cook, Does.Contain("take the image link itself, whole").And.Contain("never one from another site"));
        Assert.That(cook, Does.Contain("never a share button, a print or PDF icon, a logo or an avatar"));
        Assert.That(cook, Does.Contain("no image, and nothing said about it"));
        // FX24 (Louis, 2026-10-26: "there might be some websites who do not have images, we should just exclude them"): a page with no photo is dropped for the next result.
        Assert.That(Flat(cook), Does.Contain("Stop at the first page that has the recipe and a photo of the dish"));
        Assert.That(Flat(cook), Does.Contain("use the best recipe page of them and show no image"));
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
        Assert.That(cook, Does.Contain("`Pick n Pay Malmesbury`, `PnP in Paarl Mall`"), "a branch is named with its place");
    }

    [Test]
    public void TheCooksLabels_AreInTheTasksLanguage()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("`Dien {n} · {tyd}`").And.Contain("`Serves {n} · {time}`"), "the recipe line");
        Assert.That(cook, Does.Contain("`Skakel: {url}`").And.Contain("`Link: {url}`"), "the recipe's link");
        Assert.That(cook, Does.Not.Contain("Geprys: {n} van {m}"), "no tally: nothing is priced (F18 step 5.7)");
        Assert.That(cook, Does.Not.Contain("One line, `Serves {n} · {total time}`"), "not English alone");
        Assert.That(cook, Does.Contain("in the task's language, never the other"));
    }

    [Test]
    public void ACountOfPacks_GoesAfterThePack()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("a count after it, `× 2`"));
    }

    [Test]
    public void TheCook_ReadsTheRecipeFirst_ForAsManyPeopleAsPulsAsked()
    {
        // 2026-10-24 (Louis): "deals first, for every meal" went; the recipe, from a real page, comes first. 2026-10-25 (step 5.7): no deals either, no
        // shop is looked up; the shops pack's own guide is unchanged and waits for a retailer to say yes.
        string cook = Flat(Read("specialists/puls-cook.md"));
        string shops = Read("toolsets/shops.md");
        string router = Read("agent/puls/voice.md");

        Assert.That(cook, Does.Contain("The recipe first, from a real page.").And.Not.Contain("Deals pick the products"));
        Assert.That(cook, Does.Not.Contain("when cost matters"), "not only for a budget");
        Assert.That(cook, Does.Contain("Your first tool calls are `GetToday` and that search"));
        Assert.That(shops, Does.Contain("Look for deals first"), "the pack's guide is unchanged");
        Assert.That(shops, Does.Contain("the size the recipe needs"));
        Assert.That(router, Does.Contain("**Vir hoeveel:**").And.Contain("**Serves:**"), "its own line in every meal's brief");
        Assert.That(router, Does.Contain("Vir hoeveel mense kook jy?"), "Puls asks first, never a guess");
        Assert.That(router, Does.Contain("ask first, both in one short question"));
        Assert.That(cook, Does.Contain("cook for exactly that many").And.Not.Contain("cook for one"));
    }

    [Test]
    public void TheCook_SearchesTen_SkipsTheBlockedSites_ReadsInOrder_AndStopsAtTheFirstPageWithAPhoto()
    {
        // Louis, 2026-10-26: "search 10, skip the blocked sites, read the results in order, and stop at the first page with a recipe and a photo (at most three pages)".
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("`WebSearch` for one with `maxResults` 10"));
        Assert.That(cook, Does.Contain("Skip the results on these sites").And.Contain("`boerekos.co.za`"));
        Assert.That(cook, Does.Contain("Read the rest in the order the search gave them, the best first; never at random"));
        Assert.That(cook, Does.Contain("Stop at the first page that has the recipe and a photo of the dish"));
        Assert.That(cook, Does.Contain("Read at most three pages"));
        Assert.That(cook, Does.Contain("If none of the three has a photo, use the best recipe page of them and show no image"));
    }

    [Test]
    public void TheCook_ChoosesTheDishToFitTheBudget_BeforeItWritesTheList()
    {
        // Louis, 2026-10-26, after cook-malmesbury (R421 on a R150 budget): the budget picks the dish; it is not checked after the list is written.
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("With a budget, the dish is chosen to fit it, before the list is written"));
        Assert.That(cook, Does.Contain("Estimate the dish's total first"));
        Assert.That(cook, Does.Contain("a dish that comes out over the budget is swapped for a cheaper one"));
        Assert.That(cook, Does.Contain("Only if no reasonable dish fits does the answer go over, and then *Unsure* says by how much and why"));
        Assert.That(cook, Does.Contain("pantry staples the user has (cooking oil, salt, pepper, sugar, flour, vinegar, stock cubes and the usual dried spices and herbs)"), "listed as in the pantry, with no price");
    }

    [Test]
    public void TheCook_KeepsAMealWithinTheBudget_ByDroppingAnOverBudgetRecipe_AndNotPricingThePantry()
    {
        // Louis, 2026-10-26, after the --repeat 3 run (cook-malmesbury: R258 and R289 on R150, and a pass at R129): the cook priced every spice, sauce and sweetener as a whole pack
        // (R74 of spices in one answer, R163 of condiments in another), took the first recipe it read, and said so under the budget row. The budget rule picks the recipe, and the pantry is not bought.
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("A recipe whose estimated total is over the budget is dropped for the next page you read"));
        Assert.That(cook, Does.Contain("none of the three fits, use the cheapest of them"));
        Assert.That(cook, Does.Contain("A dish that needs a dozen small packs of spices, sauces and sweeteners is not a dish for a small budget"));
        Assert.That(cook, Does.Contain("one cheap main ingredient (mince, chicken portions, eggs, beans, a soup)"));
        Assert.That(cook, Does.Contain("pantry staples the user has (cooking oil, salt, pepper, sugar, flour, vinegar, stock cubes and the usual dried spices and herbs)"));
        Assert.That(cook, Does.Contain("*From the pantry: cooking oil, salt, pepper, sugar and spices.*"));
        Assert.That(cook, Does.Contain("*Uit die voorraadkas: kookolie, sout, peper, suiker en speserye.*"));
    }

    [Test]
    public void TheCook_LooksUpNoPrices_AndEstimatesForEveryShop()
    {
        // 2026-10-25 (Louis: "keep cook but just rework it to not lookup prices but just focus on local recipes and where to shop"): the task's Winkel / Shop
        // line names the shop to name a branch of; no shop's site is read (Checkers refuses our browser, PnP's and Woolworths' robots.txt disallow their
        // search pages, ADR 0038), and the cook loads no shops toolset: the header is gone, the pack stays for when a retailer says yes.
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.StartWith("# Puls specialist: Local cook"), "no toolsets header: the cook loads the shops no more");
        Assert.That(cook, Does.Contain("The user's shop, named").And.Contain("*Winkel* / *Shop*"));
        Assert.That(cook, Does.Contain("No shop's prices are looked up, for now: a shop's own site is read only with its permission (asked 2026-10-25)"));
        Assert.That(cook, Does.Contain("every answer is *Estimates*"));
        Assert.That(cook, Does.Not.Contain("`FindProducts`").And.Not.Contain("`SearchSpecials`").And.Not.Contain("`SearchPromotions`"));
        Assert.That(cook, Does.Not.Contain("| Checkers | `shops`").And.Not.Contain("https://www.pnp.co.za/search/").And.Not.Contain("woolworths.co.za/browse"), "no shop route");
        Assert.That(cook, Does.Not.Contain("A rendered page (Pick n Pay, Woolworths)").And.Not.Contain("*Item* is the product's own page"));
        Assert.That(cook, Does.Contain("**Don't read** any shop's site"));
        Assert.That(cook, Does.Not.Contain("pick two at random").And.Not.Contain("before any `WebSearch`"), "no shop without a tool");
        Assert.That(cook, Does.Not.Contain("Which shops' pages give a price").And.Not.Contain("soekindeks"), "no prices from the web");
    }

    [Test]
    public void EveryShop_GetsEstimates_ThatSaySo()
    {
        // 2026-10-24: instead of explaining why a shop can't be priced, the cook estimates and says so, in the task's language. Step 5.7: for every shop.
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("whichever shop the user named"));
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
        Assert.That(cook, Does.Contain("| Ingredient ").And.Contain("| For the recipe ").And.Contain("| To buy ").And.Contain("| Estimated price |"));
        Assert.That(cook, Does.Contain("one column, headed *Estimated price* /"));
        Assert.That(cook, Does.Contain("**No branch in the town:**"), "a town without the branch");
        Assert.That(cook, Does.Contain("| **Total**").And.Contain("**± R114**").And.Contain("| **Per person**"));
        Assert.That(cook, Does.Contain("Every price is `± R…` with no link"));
        Assert.That(cook, Does.Not.Contain("**Unpriced"));
        Assert.That(cook, Does.Not.Contain("### Total"));
        Assert.That(cook, Does.Not.Contain("| -  "), "no unpriced row: every price is an estimate");
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
