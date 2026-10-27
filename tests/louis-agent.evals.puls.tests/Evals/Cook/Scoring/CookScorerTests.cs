using LouisAgent.Choices;
using LouisAgent.Evals;
using LouisAgent.Evals.Puls.Evals.Cook;
using LouisAgent.Evals.Puls.Evals.Cook.Scoring;
using LouisAgent.Evals.Puls.Shared.Turns;
using LouisAgent.Evals.Puls.Shared;

namespace LouisAgent.Evals.Puls.Tests.Evals.Cook.Scoring;

// The Python's test_cook.py (F31 step 7b, F34): what Puls and its cook called, not only what they wrote, as numbers. Each
// expected value is the Python's; the cook's rules are the Python's as they are today.
public class CookScorerTests
{
    private static readonly ToolCall Start = new("StartSubSession", Json("""{"role":"puls-cook"}"""));

    private const string NoShopList = "| Bestanddeel | Vir die resep | Om te koop | Prys |\n|---|---|---|---|\n"
                                      + "| Hoender | 1 kg | Hoenderstukke | - |\n| Rys | 300 g | Rys 500 g | - |\n\n"
                                      + "*Geen winkel naby jou het nog 'n katalogus hier nie.*";

    [Test]
    public void HandedToTheCook_IsRoutedRight_WithItsShopsAndRating()
    {
        var first = Turn("| Bestanddeel | Prys |\n|---|---|\n| Meel | [R22.99](https://www.checkers.co.za/p/1) |\n| Eiers | R45 |\n",
            calls: [Start], role: "puls-cook",
            specialistCalls: [Fetch("https://www.checkers.co.za/p/1"), Fetch("https://www.pnp.co.za/p/2"), Fetch("https://www.bbc.co.uk/food")]);

        var scores = CookScorer.Score(Dev.Cook("cook-af"), [first], cookOn: true);

        Assert.That(scores.Role, Is.EqualTo("puls-cook"));
        Assert.That(scores.RoutedRight, Is.True);
        Assert.That(scores.ShopHosts, Is.EqualTo(new[] { "checkers.co.za", "pnp.co.za" }));
        Assert.That(scores.SourcesLocal, Is.False, "a UK recipe site isn't South African");
        Assert.That((scores.Priced, scores.Ingredients, scores.PricedRatio, scores.Unsourced), Is.EqualTo(((int?)1, (int?)2, (double?)0.5, (int?)1)));
        Assert.That(scores.PerKg, Is.EqualTo(0));
    }

    [Test]
    public void TheCooksShopTools_CountAndPriceAtTheShops()
    {
        // F34 step 5: the cook reads shops through its tools, never their pages, so a tool call is pricing at a shop.
        var calls = new[]
        {
            new ToolCall("FindProducts", Json("""{"shop":"checkers","query":"rice"}""")),
            new ToolCall("SearchSpecials", Json("""{"shop":"checkers","query":"rice"}""")),
            new ToolCall("SearchPromotions", Json("""{"shop":"checkers","query":"chicken"}""")),
            Fetch("https://nominatim.openstreetmap.org/search?q=Checkers+Malmesbury"),
        };

        var scores = CookScorer.Score(Dev.Cook("cook-malmesbury"), [Turn("Resep.", calls: [Start], role: "puls-cook", specialistCalls: calls)], cookOn: true);

        Assert.That((scores.ShopToolCalls, scores.DealToolCalls), Is.EqualTo((3, 2)));
        Assert.That(scores.PricedAtShops, Is.True, "no shop page read, but its catalogue asked");
        Assert.That(scores.ShopHosts, Is.EqualTo(new[] { "checkers.co.za" }), "its catalogue searched is the shop's site read");
    }

    [Test]
    public void WhichStoreAnsweredTheCooksSearches_IsScored()
    {
        string[] results =
        [
            "Products at Checkers (the store serving Malmesbury, store 655b0c8f) for 'rice' (1)",
            "Products at Checkers (its default store, store 5ece6935: Malmesbury's wasn't used) for 'onions' (1)",
        ];

        var scores = CookScorer.Score(Dev.Cook("cook-malmesbury"), [Turn("Resep.", calls: [Start], role: "puls-cook", specialistResults: results)], cookOn: true);

        Assert.That((scores.StoreForArea, scores.StoreDefault), Is.EqualTo((1, 1)));
    }

    [Test]
    public void TheScoresDocumentKeys_AreThePythonsKeys()
    {
        // A renamed property would rename the key in the scores document: this holds the Python's 27 names and the estimated count (F18, FX24).
        var element = System.Text.Json.JsonSerializer.SerializeToElement<object>(CookScorer.Score(Dev.Cook("cook-af"), [Turn()], cookOn: false), EvalScores.Json);
        Assert.That(element.EnumerateObject().Select(p => p.Name), Is.EquivalentTo(new[] { "role", "routed_right", "answered_itself", "hosts", "local_hosts", "shop_hosts", "sources_local", "priced_at_shops", "shop_tool_calls", "deal_tool_calls", "deals_named", "untooled_shops", "no_catalogue_line", "store_for_area", "store_default", "ingredients", "priced", "unsourced", "per_kg", "estimated", "images", "no_shop_rows", "wrong_store", "priced_ratio", "rand_amounts", "unconfirmed_marks", "errors", "follow_up", "follow_up_handle" }));
    }

    [Test]
    public void OneKey_InATownWithNoShop_AnUnlinkedPriceIsWrongStoreToo()
    {
        // F18 step 3.3, the one-key rule (TODO line 30): a priced row is wrong_store whether or not it carries a link, and an
        // unlinked price is wrong_store too; unsourced counts only where the town has a shop. Today's Python gives 1 and 1.
        string answer = "| Bestanddeel | Prys |\n|---|---|\n| Meel | [R22.99](https://www.checkers.co.za/p/1) |\n| Eiers | R45 |\n";

        var scores = CookScorer.Score(Dev.Cook("cook-recipe-only"), [Turn(answer)], cookOn: false);

        Assert.That((scores.WrongStore, scores.Unsourced, scores.Priced, scores.NoShopRows), Is.EqualTo((2, (int?)0, (int?)0, 0)));
    }

    [Test]
    public void TheListsRuleScores_AreReported()
    {
        string answer = "| Ingredient | For the recipe | To buy | Price |\n|---|---|---|---|\n| Rice | 300 g | Rice 1 kg | - |\n\n"
                        + "*Geen winkel naby jou het nog 'n katalogus hier nie.*";

        var scores = CookScorer.Score(Dev.Cook("cook-recipe-only"), [Turn(answer, calls: [Start], role: "puls-cook")], cookOn: true);

        Assert.That((scores.DealsNamed, scores.UntooledShops, scores.NoCatalogueLine), Is.EqualTo((0, 0, true)));
        Assert.That((scores.Priced, scores.Ingredients), Is.EqualTo(((int?)0, (int?)1)));
    }

    [Test]
    public void ACooksSources_AreItsShopsCatalogue_AndAMapIsNotOne()
    {
        // Every price from Checkers' catalogue, the branch from OpenStreetMap: all the answer's sources are South African.
        var calls = new[]
        {
            new ToolCall("FindProducts", Json("""{"shop":"checkers","query":"rice"}""")),
            Fetch("https://nominatim.openstreetmap.org/search?q=Checkers+Malmesbury"),
        };

        var scores = CookScorer.Score(Dev.Cook("cook-malmesbury"), [Turn("Resep.", calls: [Start], role: "puls-cook", specialistCalls: calls)], cookOn: true);

        Assert.That(scores.ShopHosts, Is.EqualTo(new[] { "checkers.co.za" }));
        Assert.That(scores.SourcesLocal, Is.True, "a branch looked up on a map is no source");
    }

    [Test]
    public void WhereThereIsNoShop_NoPriceIsRight()
    {
        var scores = CookScorer.Score(Dev.Cook("cook-recipe-only"), [Turn(NoShopList, calls: [Start], role: "puls-cook")], cookOn: true);

        Assert.That((scores.Priced, scores.NoShopRows, scores.WrongStore, scores.Ingredients), Is.EqualTo(((int?)0, 2, 0, (int?)2)));
        Assert.That(scores.PricedRatio, Is.EqualTo(1.0));
    }

    [Test]
    public void WhereThereIsNoShop_APriceIsFromTheWrongStore()
    {
        // Puls alone in Prince Albert priced at Checkers' default store, linked: sourced, but not where the user is.
        string answer = "| Wat | Prys |\n|---|---|\n| [Uie](https://www.checkers.co.za/product/onions-1kg-10135699EA) | R34.99 |\n| Rys | - |\n";

        var scores = CookScorer.Score(Dev.Cook("cook-recipe-only"), [Turn(answer)], cookOn: false);

        Assert.That((scores.Priced, scores.NoShopRows, scores.WrongStore, scores.Ingredients), Is.EqualTo(((int?)0, 1, 1, (int?)2)));
        Assert.That(scores.PricedRatio, Is.EqualTo(0.5));
    }

    [Test]
    public void WhereThereIsAShop_EstimatedRowsCountAsPriced_InTheRatio_ButNotInPriced()
    {
        // FX24: the cook gives estimates while the stores' answer is awaited; the ratio counts them, `priced` (the shop's own price) stays 0, and `estimated` says how many.
        string answer = "| Bestanddeel | Vir die resep | Om te koop | Geskatte prys |\n|---|---|---|---|\n"
                        + "| Wors | 500 g | boerewors 500 g | ± R55 |\n| Ui | 1 ui | ui, 1 stuk | ± R6 |\n| Sout | na smaak | sout | - |\n| Kruie | na smaak | kruie | ± R15 |\n";

        var scores = CookScorer.Score(Dev.Cook("cook-malmesbury"), [Turn(answer, calls: [Start], role: "puls-cook")], cookOn: true);

        Assert.That((scores.Priced, scores.Estimated, scores.Ingredients, scores.PricedRatio), Is.EqualTo(((int?)0, (int?)3, (int?)4, (double?)0.75)));
    }

    [Test]
    public void TheScores_CountTheRecipePhotosTheAnswerShows_NotTheShareButtons()
    {
        // FX24 (Louis, 2026-10-26: "i need to see here recipe-image"): the portal's row says whether the answer showed a photo of the dish.
        string shown = "![Bobotie](https://marindakook.co.za/wp-content/bobotie.jpg)\n\n![](https://x.co.za/wp-content/plugins/share/facebook.png)\n\nDien 4";

        Assert.That(CookScorer.Score(Dev.Cook("cook-af"), [Turn(shown, calls: [Start], role: "puls-cook")], cookOn: true).Images, Is.EqualTo(1));
        Assert.That(CookScorer.Score(Dev.Cook("cook-af"), [Turn("Dien 4", calls: [Start], role: "puls-cook")], cookOn: true).Images, Is.EqualTo(0));
    }

    [Test]
    public void WhereThereIsNoShop_EstimatedRowsAreEstimates_NotNoShopRows()
    {
        // FX24 (Louis, 2026-10-26, on cook-no-branch and cook-recipe-only: "these ones did price and it is fine, it's estimates"): the cook gives estimates in every town, since no shop's
        // price is read. An estimate in a town with no shop is no price from a store that isn't the town's: it is counted as an estimate, as anywhere, not as "no shop, no price".
        string answer = "| Bestanddeel | Vir die resep | Om te koop | Geskatte prys |\n|---|---|---|---|\n"
                        + "| Wors | 500 g | boerewors 500 g | ± R55 |\n| Ui | 1 ui | ui, 1 stuk | ± R6 |\n| Sout | na smaak | sout | - |\n";

        var scores = CookScorer.Score(Dev.Cook("cook-recipe-only"), [Turn(answer, calls: [Start], role: "puls-cook")], cookOn: true);

        Assert.That((scores.Estimated, scores.NoShopRows, scores.WrongStore, scores.Ingredients), Is.EqualTo(((int?)2, 1, 0, (int?)3)));
        Assert.That(scores.PricedRatio, Is.EqualTo(1.0));
    }

    [Test]
    public void WhereThereIsAShop_ADashIsStillNoPrice()
    {
        var scores = CookScorer.Score(Dev.Cook("cook-malmesbury"), [Turn(NoShopList, calls: [Start], role: "puls-cook")], cookOn: true);

        Assert.That((scores.Priced, scores.NoShopRows, scores.PricedRatio), Is.EqualTo(((int?)0, 0, (double?)0.0)));
    }

    [Test]
    public void AnsweredByPulsItself_IsRoutedWrongWhenTheCookWasOn()
    {
        var scores = CookScorer.Score(Dev.Cook("cook-en"), [Turn("A recipe.")], cookOn: true);

        Assert.That(scores.RoutedRight, Is.False);
        Assert.That(scores.AnsweredItself, Is.True);
    }

    [Test]
    public void WithoutTheCook_PulsCookingItselfIsRight_AndItsOwnReadsCount()
    {
        var scores = CookScorer.Score(Dev.Cook("cook-af"), [Turn("R50", calls: [Fetch("https://www.checkers.co.za/p/1")])], cookOn: false);

        Assert.That(scores.RoutedRight, Is.True);
        Assert.That(scores.ShopHosts, Is.EqualTo(new[] { "checkers.co.za" }));
    }

    [Test]
    public void Continued_IsTheSameCook()
    {
        var scores = CookScorer.Score(Dev.Cook("cook-af"), [Turn(calls: [Start], role: "puls-cook"), Turn(continued: "sub-1")], cookOn: true);

        Assert.That((scores.FollowUp, scores.FollowUpHandle), Is.EqualTo(("continued", "sub-1")));
    }

    [Test]
    public void ANewCook_IsNotContinued()
    {
        var scores = CookScorer.Score(Dev.Cook("cook-af"), [Turn(calls: [Start], role: "puls-cook"), Turn(calls: [Start], role: "puls-cook")], cookOn: true);

        Assert.That(scores.FollowUp, Is.EqualTo("new sub-session"));
    }

    [Test]
    public void OneQuestion_HasNoFollowUp() =>
        Assert.That(CookScorer.Score(Dev.Cook("cook-shops"), [Turn()], cookOn: true).FollowUp, Is.Null);

    [Test]
    public void AWebQuestionPulsAnswersItself_IsRight() =>
        Assert.That(CookScorer.Score(Dev.Cook("control-web"), [Turn("Die koers is 10.5%.")], cookOn: true).RoutedRight, Is.True);

    [Test]
    public void HandedToTheCook_IsWrong() =>
        Assert.That(CookScorer.Score(Dev.Cook("control-web"), [Turn(calls: [Start], role: "puls-cook")], cookOn: true).RoutedRight, Is.False);

    [Test]
    public void ErrorsAreCounted_AndAnErroredTurnIsNeverRight()
    {
        var scores = CookScorer.Score(Dev.Cook("control-web"), [Turn(error: "overloaded")], cookOn: true);

        Assert.That(scores.Errors, Is.EqualTo(1));
        Assert.That(scores.RoutedRight, Is.False);
    }

    [Test]
    public void CanDelegate_IsTheSpecialistsToolset_WhateverTheModeIsNamed()
    {
        // The Python's rule: "specialists" in the mode's toolsets. A mode named for the cook with no cook is not on; a
        // differently named mode with the cook is.
        Assert.That(PulsModes.CanDelegate(new EvalMode("puls-cook-sonnet", "puls-cook", [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()])), Is.True);
        Assert.That(PulsModes.CanDelegate(new EvalMode(CookMode.Cook.Wire(), "puls-cook", [PulsToolset.Web.Wire(), PulsToolset.Shops.Wire()])), Is.False);
        Assert.That(PulsModes.CanDelegate(new EvalMode(CookMode.Tools.Wire(), "puls-cook", [PulsToolset.Web.Wire(), PulsToolset.Shops.Wire()])), Is.False);
    }

    [Test]
    public void TheModes_ArePulsWithTheCooksToolsAndPulsWithItsCook() =>
        Assert.That(CookSetup.Modes, Is.EqualTo(new Dictionary<string, IReadOnlyList<string>>
        {
            [CookMode.Tools.Wire()] = [PulsToolset.Web.Wire(), PulsToolset.Shops.Wire()],
            [CookMode.Cook.Wire()] = [PulsToolset.Specialists.Wire(), PulsToolset.Web.Wire()],
        }));

    private static TurnView Turn(string answer = "", ToolCall[]? calls = null, ToolCall[]? specialistCalls = null, string? role = null,
        string? continued = null, string? error = null, string[]? specialistResults = null) =>
        new(answer, calls ?? [], specialistCalls ?? [], role, continued, error, [], specialistResults ?? []);

    private static ToolCall Fetch(string url) => new("FetchUrl", Json("{\"url\":\"" + url + "\"}"));

    private static System.Text.Json.JsonElement Json(string text) => System.Text.Json.JsonDocument.Parse(text).RootElement.Clone();
}
