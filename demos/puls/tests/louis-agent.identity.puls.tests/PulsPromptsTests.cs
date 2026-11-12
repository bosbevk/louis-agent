using LouisAgent.Choices;
using LouisAgent.Composition.Agents;
using LouisAgent.Composition.Packs;
using LouisAgent.Core.Delegation.Addresses;
using LouisAgent.Core.Hosting.Prompts;
using LouisAgent.Core.Config;
using LouisAgent.Core.Skills;
using LouisAgent.Core.Skills.Headers;
using LouisAgent.Core.Skills.Roles;
using LouisAgent.Identities.Puls.Evals.Checks.Shared;
using LouisAgent.Packs.Recipes;

namespace LouisAgent.Identities.Puls.Tests;

/// <summary>
/// Puls's own prompts, read the way the Puls stack registers them: its modes, its specialists and minions, and the
/// rules each prompt has to carry. They came from core's role tests when the identity moved out (CL15 step 3).
/// </summary>
public class PulsPromptsTests
{
    /// <summary>Every level-0 mode this test run offers, core's and Puls's.</summary>
    private const string Modes = "puls/voice-af, puls/voice-en";

    /// <summary>One prompt as the model reads it, through the same readers the engine uses: its body (CL27).</summary>
    private static string Read(string path) => PromptHeaders.Split(path, Raw(path)).Body;

    /// <summary>One prompt's file, header and all.</summary>
    private static string Raw(string path) =>
        SkillFileSource.ReadEmbedded(path) ?? throw new FileNotFoundException(path);

    /// <summary>A prompt with its line breaks and indents as single spaces, for a phrase the file wraps.</summary>
    private static string Flat(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Puls's prompts live in its identity now; core holds none of them.</summary>
    [Test]
    public void PulsPrompts_ComeFromItsIdentity_NotFromCore()
    {
        // The red line of the move: the identity's own provider has the cook, and core's prompts don't.
        Assert.That(new PulsIdentity().Prompts.GetFileInfo("specialists/puls-cook.md").Exists, Is.True);
        Assert.That(SkillFileSource.CoreFiles, Has.None.Contains("puls"));
        Assert.That(Read("specialists/puls-cook.md"), Does.Contain("Local cook"), "read through the one reader");
    }

    /// <summary>With Puls registered, its modes, specialists and minion are listed, and each one loads.</summary>
    [Test]
    public void PulsAndItsSpecialists_AreListed_WithPulsRegistered()
    {
        Assert.That(RoleCatalog.Available(RoleLevel.Agent), Is.EqualTo(Modes.Split(", ")));
        Assert.That(RoleCatalog.Available(RoleLevel.Specialist),
            Is.EqualTo(new[]
            {
                PulsRole.Analyst.Wire(), PulsRole.Cook.Wire(), PulsRole.Historian.Wire(), PulsRole.Reader.Wire(),
                PulsRole.Researcher.Wire()
            }));
        Assert.That(RoleCatalog.Available(RoleLevel.Minion),
            Is.EquivalentTo(new[] { PulsRole.MinionWebPriceLookup.Wire(), PulsRole.MinionFindRecipe.Wire() }));
        Assert.That(SkillLoader.Load(new AgentOptions(), [], "puls/voice-af").Documentation,
            Does.Contain("# Puls - Stem"));
        Assert.That(SkillLoader.Load(new AgentOptions(), [], PulsRole.Cook.Wire(), RoleLevel.Specialist).Documentation,
            Does.Contain("Prices are Stats SA's averages"));
        Assert.That(SkillLoader.ToolsetsOf(PulsRole.Cook.Wire(), RoleLevel.Specialist),
            Is.EqualTo(new[]
            {
                PulsToolset.Web.Wire(), RecipesToolset.Recipes.Wire(), PulsToolset.WhereToBuy.Wire(),
                PulsToolset.ShopPrices.Wire(), PulsToolset.Calculator.Wire()
            }),
            "the cook declares the web, recipes, where to buy, shop prices and the calculator in its header " +
            "(ADR 0025)");
    }

    /// <summary>
    /// A language loads its own voice first, then Puls's shared routing, which is no mode of its own.
    /// </summary>
    [TestCase("af", "# Puls - Stem")]
    [TestCase("en", "# Puls - Voice")]
    public void APulsLanguage_LoadsItsVoice_ThenPulsShared(string code, string voice)
    {
        string docs = SkillLoader.Load(new AgentOptions(), [], $"puls/voice-{code}").Documentation;

        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal), Is.GreaterThan(0));
        Assert.That(docs.IndexOf(voice, StringComparison.Ordinal),
            Is.LessThan(docs.IndexOf("# Puls - Routing", StringComparison.Ordinal)));
        Assert.That(Read("agent/puls/voice.md"), Does.StartWith("# Puls - Routing"), "Puls's shared part");
        Assert.Throws<RoleException>(() => SkillLoader.Load(new AgentOptions(), [], "puls/voice"),
            "the shared part is no mode of its own");
    }

    /// <summary>The two languages say the same things: the same sections and the same number of points.</summary>
    [Test]
    public void ThePulsLanguages_HaveTheSameSections()
    {
        // The headings of one voice, in order.
        static string[] Sections(string text) => text.Split('\n').Where(l => l.StartsWith("## ")).ToArray();
        string af = Read("agent/puls/voice-af.md"), en = Read("agent/puls/voice-en.md");

        Assert.That(Sections(en), Has.Length.EqualTo(Sections(af).Length));
        Assert.That(en.Split('\n').Count(l => l.StartsWith("- ")),
            Is.EqualTo(af.Split('\n').Count(l => l.StartsWith("- "))), "the same points");
    }

    /// <summary>Each voice opens with its heading, in its own language.</summary>
    [TestCase("agent/puls/voice-en.md", "# Puls - Voice")]
    [TestCase("agent/puls/voice-af.md", "# Puls - Stem")]
    public void AVoicesHeading_SaysVoice(string file, string heading) =>
        Assert.That(Read(file), Does.StartWith(heading));

    /// <summary>Puls's routing names only what Puls does, and carries its own briefing and relay rules.</summary>
    [Test]
    public void Puls_RoutesOnlyWhatItDoes_AndCarriesItsOwnProtocol()
    {
        // CL9 step 3: a product's routing holds its own rows. CL15 step 3: and its own briefing, roles and relay lines.
        string puls = Read("agent/puls/voice.md");

        Assert.That(puls,
            Does.Contain("`web`").And.Contain("`agents`").And.Contain("`puls.cook`").And.Contain("`Wants`"));
        // Its routing table (the rows) names no coding toolset; the analyst's counting rule, moved here, may name
        // files and python.
        string rows = string.Join("\n", puls.Split('\n').Where(r => r.StartsWith('|')));
        foreach (string coding in new[]
                 {
                     "`files`", "`git`", "`dotnet`", "`python`", "`paymo`", "`devops`", "`skills`"
                 })
            Assert.That(rows, Does.Not.Contain(coding), $"Puls doesn't route {coding}");
        Assert.That(puls, Does.Contain("isn't available here yet"), "and still says so in one line when asked");
        Assert.That(puls, Does.Contain("## Briefing the specialist").And.Contain("## Relaying a report"),
            "the protocol, moved from the shared guide");
    }

    /// <summary>A minion's prompt is the principles, then what every minion is, then its own role.</summary>
    [Test]
    public void AMinionsPrompt_IsThePrinciples_TheBase_ThenItsRole()
    {
        string docs = SkillLoader.Load(new AgentOptions(), [], PulsRole.MinionWebPriceLookup.Wire(), RoleLevel.Minion)
            .Documentation;

        int principles = docs.IndexOf("# Principles", StringComparison.Ordinal);
        int @base = docs.IndexOf("You are a **minion**", StringComparison.Ordinal);
        int role = docs.IndexOf("# Puls minion: Web price lookup", StringComparison.Ordinal);
        Assert.That(principles, Is.GreaterThanOrEqualTo(0));
        Assert.That(@base, Is.GreaterThan(principles), "the base after the principles");
        Assert.That(role, Is.GreaterThan(@base), "the role after the base");
        Assert.That(RoleCatalog.MinionBaseOf(PulsRole.MinionWebPriceLookup.Wire()), Is.EqualTo("puls-minion"));
    }

    /// <summary>
    /// The cook no longer hands work to a price-lookup minion, though the minion role stays for later.
    /// </summary>
    [Test]
    public void TheCook_NamesNoPriceLookupMinion_NowThatItLooksUpNoPrices()
    {
        // F18 step 5.7: the cook's minion table (CL15 step 3) handed one ingredient to a price-lookup minion; with no
        // prices looked up it is gone from the cook. The minion role stays, with its tests, for when a retailer says
        // yes.
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Not.Contain(PulsRole.MinionWebPriceLookup.Wire()).And.Not.Contain("## Minions"));
        Assert.That(RoleCatalog.Available(RoleLevel.Minion),
            Does.Contain(PulsRole.MinionWebPriceLookup.Wire()), "the role stays");
    }

    /// <summary>
    /// Counting goes to the analyst, and the reader hands a counting task back instead of reading it all.
    /// </summary>
    [Test]
    public void CountingGoesToTheAnalyst_AndTheReaderHandsItBack()
    {
        // FX13: a counting task sent to a files-only reader read every test file into a 298k-token prompt ($0.278).
        string router = Read("agent/puls/voice.md");
        string reader = Read("specialists/puls-reader.md");

        Assert.That(router, Does.Contain("Counting is the analyst's, never the reader's"));
        Assert.That(reader, Does.Contain("Don't count by reading").And.Contain("needs the analyst"));
    }

    /// <summary>
    /// Every specialist is told that each tool call costs money, and to match the tone of the note it got.
    /// </summary>
    [TestCase(PulsRole.Researcher)]
    [TestCase(PulsRole.Reader)]
    [TestCase(PulsRole.Historian)]
    [TestCase(PulsRole.Analyst)]
    [TestCase(PulsRole.Cook)]
    public void EverySpecialist_IsToldWhatItsToolCallsCost_AndMatchesTheNotesTone(PulsRole role)
    {
        // FX13: the reader that read 74 files never saw the router; the rule has to be in the role it runs with.
        string text = Read($"specialists/{role.Wire()}.md");

        Assert.That(text, Does.Contain("Use the fewest tool calls that answer the task").And.Contain("paid for again"));
        Assert.That(text, Does.Contain("how the user sounds").And.Contain("keep your report's shape"));
    }

    /// <summary>
    /// The routing teaches the model to hand work on by address (F127.3.1), never by the old tools' toolsets and role.
    /// </summary>
    [Test]
    public void ThePromptsTeach_AskingBySpecialistsAddress()
    {
        string router = Read("agent/puls/voice.md");

        Assert.That(router, Does.Contain("AskAgent(address, message)").And.Not.Contain("StartSubSession"));
        Assert.That(router, Does.Contain("Pass its address").And.Contain("ROLES_ENABLED"));
    }

    /// <summary>
    /// Puls may ask its five specialists by address, and has the cook's report shown whole, as ADR 0079 did for its
    /// specialists (F127.3.1, Louis's pick A); its minions are listed beside them (F127.5).
    /// </summary>
    [Test]
    public void Puls_CallsItsFiveSpecialists_AndRelaysTheirAnswersWhole()
    {
        IAgentIdentity puls = new PulsIdentity();

        Assert.Multiple(() =>
        {
            Assert.That(puls.Calls.Where(call => !AgentAddress.TryParse(call, out var address) ||
                                                 !AgentAddresses.IsMinion(address)),
                Is.EquivalentTo(
                    new[] { PulsRole.Cook, PulsRole.Researcher, PulsRole.Analyst, PulsRole.Historian, PulsRole.Reader }
                        .Select(role => role.Wire().Replace($"{puls.Name}-", $"{puls.Name}."))),
                "each address reaches its specialist's file, specialists/puls-*.md");
            Assert.That(puls.RelaysAnswersWhole, Is.True);
        });
    }

    /// <summary>The cook answers for the user's own town and quotes no price as if it were checked.</summary>
    [Test]
    public void TheCook_AnswersForWhereTheUserIs_AndQuotesNoPrice()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook,
            Does.Contain("Where the user is").And.Contain("never your own guess, never a search snippet"));
        Assert.That(cook, Does.Contain("every ingredient named in the task's language"));
        Assert.That(cook, Does.Contain("no brand, no photo"));
        Assert.That(cook, Does.Contain("Look the user's shop up in their town, once, with `FindBranches`"));
        Assert.That(cook, Does.Contain("| Chicken thighs ").And.Contain("| 600 g ").And.Contain("chicken thighs 1 kg"));
    }

    /// <summary>
    /// The cook shows one photo of the dish, taken whole from the recipe page it read, or none at all.
    /// </summary>
    [Test]
    public void TheCook_ShowsOneImageOfTheDish_FromTheRecipePageItRead()
    {
        // Louis, 2026-10-25: "i want at least one image to render from the recipe". FetchUrl and ReadRenderedPage both
        // return images as `![alt](url)`.
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("one image of the dish").And.Contain("![Bobotie](https://…)"));
        Assert.That(cook, Does.Contain("line, whole, and never any other image"));
        Assert.That(cook, Does.Contain("no image, and nothing said about it"));
        // The stop rule (a print with no photo) is PulsCookRecipeToolsTests', now that it reads prints.
    }

    /// <summary>
    /// A follow-up goes back to the specialist that is still open, even when Puls could answer it itself.
    /// </summary>
    [Test]
    public void AFollowUp_GoesToTheOpenSpecialist()
    {
        // F26 step 4: the prompt says when to continue and when to start over; the cook check showed the rule had to
        // be firm.
        string router = Read("agent/puls/voice.md");

        Assert.That(router, Does.Contain("`AskAgent` again with the same address"));
        Assert.That(router, Does.Contain("even when you could answer it yourself"));
    }

    /// <summary>Every cooking question goes to the cook, and the cook says which branch to buy at.</summary>
    [Test]
    public void EveryCookingQuestion_GoesToTheCook_WhichSaysWhereToBuy()
    {
        string router = Read("agent/puls/voice.md");
        string cook = Read("specialists/puls-cook.md");

        Assert.That(router, Does.Contain("every cooking question").And.Contain("never from memory"));
        Assert.That(cook, Does.Contain("Where to buy").And.Contain("branches"));
        Assert.That(Flat(cook), Does.Contain("*Branches from © OpenStreetMap contributors.*"),
            "the map's licence (ODbL) asks for the credit wherever a branch is named (F50.2)");
        Assert.That(cook, Does.Contain("`FindBranches`").And.Contain("never fetch it, the link is for the user"));
        Assert.That(cook, Does.Contain("`Pick n Pay Malmesbury`, `PnP in Paarl Mall`"),
            "a branch is named with its place");
    }

    /// <summary>The cook's labels come in the task's language, both ways, never the other one.</summary>
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

    /// <summary>How many packs to buy is written after the pack, as <c>× 2</c>.</summary>
    [Test]
    public void ACountOfPacks_GoesAfterThePack()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("a count after it, `× 2`"));
    }

    /// <summary>The cook reads a real recipe first, for exactly as many people as Puls asked about.</summary>
    [Test]
    public void TheCook_ReadsTheRecipeFirst_ForAsManyPeopleAsPulsAsked()
    {
        // 2026-10-24 (Louis): "deals first, for every meal" went; the recipe, from a real page, comes first.
        // 2026-10-25 (step 5.7): no deals either, no shop is looked up. F50.2: the shops pack is gone; the list is
        // priced by the shop-prices pack, written first and estimated once.
        string cook = Flat(Read("specialists/puls-cook.md"));
        string prices = Flat(Read("toolsets/shop-prices.md"));
        string afrikaans = Read("agent/puls/voice-af.md");

        Assert.That(cook,
            Does.Contain("The recipe first, from a real page.").And.Not.Contain("Deals pick the products"));
        Assert.That(cook, Does.Not.Contain("when cost matters"), "not only for a budget");
        Assert.That(cook, Does.Contain("Your first tool calls, together in one round, are `GetToday`, `FindRecipes`"));
        Assert.That(prices, Does.Contain("Write the list first,"), "the pack's guide prices a written list");
        Assert.That(cook, Does.Contain("Once the list is written, call `EstimatePrices` once"));
        Assert.That(afrikaans, Does.Contain("**Vir hoeveel:**"), "its own line in a brief with a number");
        Assert.That(Read("agent/puls/voice-en.md"), Does.Contain("**Serves:**"), "and in English");
        Assert.That(afrikaans, Does.Contain("Vir hoeveel mense kook jy?"), "Puls asks first, never a guess");
        Assert.That(cook, Does.Contain("cook for exactly that many").And.Not.Contain("cook for one"));
    }

    /// <summary>
    /// There's one rule for when to stop reading pages, and the budget counts for more than the photo.
    /// </summary>
    [Test]
    public void TheCook_HasOneStopRule_TheBudgetWinsOverThePhoto()
    {
        // FX24 round 3 (the ccecce0 review): the search said "stop at the first page with the recipe and a photo", the
        // budget paragraph said "an over-budget recipe is dropped for the next page". One rule now, in the search
        // section: the budget is required, the photo only breaks a tie between pages that fit; the budget paragraph
        // points to it.
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Not.Contain("Stop at the first page that has the recipe and a photo"),
            "the old rule is gone");
        Assert.That(cook, Does.Contain("and, with a budget, an estimated total within it"));
        Assert.That(cook, Does.Contain("If none of the three fits the budget, use the cheapest of them"));
        Assert.That(cook, Does.Contain("A photo only breaks a tie between prints that fit"));
        Assert.That(cook, Does.Contain("The stop rule under *The recipe first* decides which page to use"));
        Assert.That(cook, Does.Not.Contain("is dropped for the next page you read, within the three pages"),
            "the budget paragraph no longer says when to stop");
        Assert.That(cook, Does.Not.Contain("If none of the three fits, use the cheapest of them"),
            "said once, in the search section");
    }

    /// <summary>With a budget, the cook picks a dish that fits it before writing the list, not after.</summary>
    [Test]
    public void TheCook_ChoosesTheDishToFitTheBudget_BeforeItWritesTheList()
    {
        // Louis, 2026-10-26, after cook-malmesbury (R421 on a R150 budget): the budget picks the dish; it is not
        // checked after the list is written.
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("With a budget, the dish is chosen to fit it, before the list is written"));
        Assert.That(cook, Does.Contain("Estimate the dish's total first"));
        Assert.That(cook, Does.Contain("a dish that comes out over the budget is swapped for a cheaper one"));
        Assert.That(cook,
            Does.Contain(
                "Only if no reasonable dish fits does the answer go over, and then *Unsure* says by how much and why"));
    }

    /// <summary>
    /// We never know what's in someone's cupboard, so every ingredient is a row with a price, spices and salt too.
    /// Only tap water stays out, because nobody buys it.
    /// </summary>
    [Test]
    public void TheCook_ListsEveryIngredient_AndAssumesNoPantry()
    {
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("Every ingredient is a row, the oil, salt, pepper and spices too"));
        Assert.That(cook, Does.Contain("Only tap water is never a row"));
        Assert.That(cook, Does.Contain("Every ingredient counts towards the budget"));
        Assert.That(cook, Does.Not.Contain("pantry").IgnoreCase);
        Assert.That(cook, Does.Not.Contain("voorraadkas").IgnoreCase);
    }

    /// <summary>The cook keeps a meal within its budget by dropping a recipe that goes over.</summary>
    [Test]
    public void TheCook_KeepsAMealWithinTheBudget_ByDroppingAnOverBudgetRecipe()
    {
        // Louis, 2026-10-26, after the --repeat 3 run (cook-malmesbury: R258 and R289 on R150, and a pass at R129):
        // the cook took the first recipe it read and said so under the budget row. The budget rule picks the recipe.
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("a recipe over the budget is dropped for the next page"));
        Assert.That(cook, Does.Contain("use the cheapest of them"));
        Assert.That(cook,
            Does.Contain(
                "A dish that needs a dozen small packs of spices, sauces and sweeteners is not a dish for a small " +
                "budget"));
        Assert.That(cook, Does.Contain("one cheap main ingredient (mince, chicken portions, eggs, beans, a soup)"));
        Assert.That(cook, Does.Contain("`ScaleRecipe`").And.Contain("never sums in your head"),
            "the scaling is a tool's job");
        Assert.That(cook, Does.Match(@"toolsets: [^\r\n]*calculator"), "and the cook has the tool");
    }

    /// <summary>
    /// Puls never asks where someone will shop: the prices are the same estimates wherever they buy. The file said both
    /// for a while, and the model followed the line that asked.
    /// </summary>
    [Test]
    public void Puls_DoesNotAskWhereTheyWillShop()
    {
        string voice = Flat(Read("agent/puls/voice.md"));

        Assert.That(voice, Does.Not.Contain("where they'll shop: ask first"));
        Assert.That(voice, Does.Contain("brief the cook with *any* and never ask"));
    }

    /// <summary>
    /// A refused page is no print, so it can't use up the cook's reads: three refusals in a row left it answering with
    /// no recipe, with hits and searches still to try. It keeps going until it has one.
    /// </summary>
    [Test]
    public void TheCook_KeepsLookingUntilItHasARecipe()
    {
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("a page that says it has no recipe we can read is no print and doesn't count"));
        Assert.That(cook, Does.Contain("Keep going until you have a recipe"));
        Assert.That(cook, Does.Contain("Never answer without a recipe while any of those is left"));
    }

    /// <summary>
    /// The cook reads no shop's site; every price it gives is an estimate, from Stats SA's averages.
    /// </summary>
    [Test]
    public void TheCook_LooksUpNoPrices_AndEstimatesForEveryShop()
    {
        // 2026-10-25 (Louis: "keep cook but just rework it to not lookup prices but just focus on local recipes and
        // where to shop"): the task's Winkel / Shop line names the shop to name a branch of; no shop's site is read
        // (Checkers refuses our browser, PnP's and Woolworths' robots.txt disallow their search pages, ADR 0038), and
        // the cook loads no shops toolset. F50.2 (Louis, 2026-11-04: "scrap it they never came back"): the shops pack
        // is gone; the cook loads where-to-buy for the branch and shop-prices for Stats SA's averages.
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(PromptHeaders.Split("specialists/puls-cook.md", Raw("specialists/puls-cook.md")).Header!.Toolsets,
            Is.EqualTo(new[]
            {
                PulsToolset.Web.Wire(), RecipesToolset.Recipes.Wire(), PulsToolset.WhereToBuy.Wire(),
                PulsToolset.ShopPrices.Wire(), PulsToolset.Calculator.Wire()
            }),
            "its header names the web (Puls no longer sends it, F127.3.1), recipes, both packs and the calculator, " +
            "and no shops");
        Assert.That(cook, Does.Contain("The user's shop, named").And.Contain("*Winkel* / *Shop*"));
        Assert.That(cook, Does.Contain("No shop's prices are looked up: no shop has given us its prices."));
        Assert.That(cook, Does.Contain("Every price comes from `EstimatePrices`, Stats SA's average prices"));
        Assert.That(cook, Does.Contain("every answer is *Estimates*"));
        Assert.That(cook,
            Does.Not.Contain("`FindProducts`").And.Not.Contain("`SearchSpecials`").And.Not
                .Contain("`SearchPromotions`"));
        Assert.That(cook,
            Does.Not.Contain("| Checkers | `shops`").And.Not.Contain("https://www.pnp.co.za/search/").And.Not
                .Contain("woolworths.co.za/browse"), "no shop route");
        Assert.That(cook,
            Does.Not.Contain("A rendered page (Pick n Pay, Woolworths)").And.Not
                .Contain("*Item* is the product's own page"));
        Assert.That(cook, Does.Contain("**Don't read** any shop's site"));
        Assert.That(cook, Does.Not.Contain("pick two at random").And.Not.Contain("before any `WebSearch`"),
            "no shop without a tool");
        Assert.That(cook, Does.Not.Contain("Which shops' pages give a price").And.Not.Contain("soekindeks"),
            "no prices from the web");
    }

    /// <summary>Whichever shop the user named, the cook gives estimates and says plainly that they are.</summary>
    [Test]
    public void EveryShop_GetsEstimates_ThatSaySo()
    {
        // 2026-10-24: instead of explaining why a shop can't be priced, the cook estimates and says so, in the task's
        // language. Step 5.7: for every shop.
        string cook = Flat(Read("specialists/puls-cook.md"));

        Assert.That(cook, Does.Contain("whichever shop the user named"));
        Assert.That(cook, Does.Contain("written `± R65` with no link"));
        Assert.That(cook,
            Does.Contain(
                "*Prices are estimates from Stats SA's average prices (the Western Cape, August 2026), not checked at a shop.*"));
        Assert.That(cook,
            Does.Contain(
                "*Pryse is skattings uit Stats SA se gemiddelde pryse (Wes-Kaap, Augustus 2026), nie by 'n winkel nagegaan nie.*"));
        Assert.That(cook, Does.Not.Contain("public API"));
    }

    /// <summary>The cook writes its report in one fixed shape, so Puls can hand it on exactly as it is.</summary>
    [Test]
    public void TheCook_WritesItsReportInOneShape_ThatPulsHandsOn()
    {
        string cook = Read("specialists/puls-cook.md");

        Assert.That(cook, Does.Contain("Puls hands your report to the user as it is"));
        Assert.That(cook, Does.Not.Contain("Puls will put it in its own voice"));
        Assert.That(cook,
            Does.Contain("| Ingredient ").And.Contain("| For the recipe ").And.Contain("| To buy ").And
                .Contain("| Estimated price |"));
        Assert.That(cook, Does.Contain("one column, headed *Estimated price* /"));
        Assert.That(cook, Does.Contain("**None marked yes:**"), "a town without the branch");
        Assert.That(cook, Does.Contain("| **Total**").And.Contain("**± R114**").And.Contain("| **Per person**"));
        Assert.That(cook, Does.Contain("Every price is `± R…` with no link"));
        Assert.That(cook, Does.Not.Contain("**Unpriced"));
        Assert.That(cook, Does.Not.Contain("### Total"));
        Assert.That(cook, Does.Not.Contain("| -  "), "no unpriced row: every price is an estimate");
        Assert.That(cook, Does.Contain("don't list them here too"));
        Assert.That(cook, Does.Contain("| Shop | Branch | Street or mall | Website |"));
        Assert.That(cook,
            Does.Contain("the branch's own page on the shop's site").And.Contain("else the shop's home page"));
        Assert.That(cook, Does.Contain("| What | Why |"));
        int dish = cook.IndexOf("### The dish", StringComparison.Ordinal),
            list = cook.IndexOf("### Shopping list", StringComparison.Ordinal),
            buy = cook.IndexOf("### Where to buy", StringComparison.Ordinal),
            unsure = cook.IndexOf("### Unsure", StringComparison.Ordinal);
        Assert.That(dish, Is.GreaterThan(-1).And.LessThan(list));
        Assert.That(list, Is.LessThan(buy));
        Assert.That(buy, Is.LessThan(unsure));
    }

    /// <summary>
    /// Puls hands the cook's report on whole, says one line like a person before and after, and treats the footer as
    /// its own, never the user's.
    /// </summary>
    [Test]
    public void Puls_RelaysTheCooksReport_HandsOffLikeAPerson_AndIntroducesIt()
    {
        string voice = Read("agent/puls/voice-af.md");
        string router = Read("agent/puls/voice.md");

        Assert.That(router, Does.Contain("as the cook wrote it").And.Contain("you aren't asked to"),
            "the report goes straight to the user, so the one choosing doesn't retype it");
        Assert.That(Flat(voice), Does.Contain("in jou eie woorde"), "the line before the call, in Puls's words");
        Assert.That(router, Does.Not.Contain("My kok is terug"), "and none above a report it doesn't write");
        Assert.That(router, Does.Contain("[Answered by puls.cook, did:"),
            "the line's example names its own cook, and no cost (FX27)");
        Assert.That(voice, Does.Not.Contain("Moet nooit 'n spesialis se verslag net so aanstuur nie"));
        Assert.That(voice, Does.Contain("nou-nou terug"), "what the hand-off says, in Puls's voice");
        Assert.That(router, Does.Contain("one line to the user first"));
    }

    /// <summary>The cook is the only role Puls hands work to, until the other specialists pass their check.</summary>
    [Test]
    public void OnlyTheCook_IsARoleToDelegate_UntilTheOthersPassTheirCheck()
    {
        // F29 step 3 (Louis: "disable all specialists, Puls boots with web, except the cook"): the prompts match the
        // gate.
        string router = Read("agent/puls/voice.md");

        string[] rows = router.Split('\n').Where(r => r.StartsWith('|')).ToArray();
        Assert.That(rows.Single(r => r.Contains("The world:")),
            Does.Contain("you, with `web`").And.Not.Contain(PulsRole.Researcher.Wire()));
        Assert.That(rows.Where(r => r.Contains("`puls.")), Has.Exactly(3).Items.And.All.Contains("`puls.cook`"),
            "the routing row and the two specialists' rows (cooking, shopping)");
        Assert.That(rows.Single(r => r.Contains("`puls.cook`") && r.Contains("`agents`") && r.Contains("Cooking")),
            Is.Not.Null, "a meal goes to the cook, asked by its address");
        Assert.That(router, Does.Contain("never hand anything else to a specialist"));
        Assert.That(router, Does.Contain("isn't available yet"), "what to say instead of delegating");
        Assert.That(router, Does.Contain("### Waiting for validation").And.Contain("ROLES_ENABLED"));
    }
}
