"""The cook check's scoring (F31 step 7b): what Puls and its cook called, not only what they wrote, as numbers."""
import unittest

from benchmarks.cook import run_check as cook


def turn(answer="", calls=(), specialist_calls=(), role=None, continued=None, error=None, specialist_results=()):
    return {"answer": answer, "calls": list(calls), "specialist_calls": list(specialist_calls), "role": role,
            "continued": continued, "error": error, "seconds": 1.0, "results": [], "specialist_results": list(specialist_results)}


def fetch(url):
    return {"name": "FetchUrl", "input": {"url": url}}


START = {"name": "StartSubSession", "input": {"role": "puls-cook"}}


class AMealQuestion(unittest.TestCase):
    def test_handed_to_the_cook_is_routed_right_with_its_shops_and_rating(self):
        first = turn("| Bestanddeel | Prys |\n|---|---|\n| Meel | [R22.99](https://www.checkers.co.za/p/1) |\n| Eiers | R45 |\n", calls=[START], role="puls-cook",
                     specialist_calls=[fetch("https://www.checkers.co.za/p/1"), fetch("https://www.pnp.co.za/p/2"), fetch("https://www.bbc.co.uk/food")])
        scores = cook.score("cook-af", [first], cook_on=True)

        self.assertEqual(scores["role"], "puls-cook")
        self.assertTrue(scores["routed_right"])
        self.assertEqual(scores["shop_hosts"], ["checkers.co.za", "pnp.co.za"])
        self.assertFalse(scores["sources_local"], "a UK recipe site isn't South African")
        self.assertEqual((scores["priced"], scores["ingredients"], scores["priced_ratio"], scores["unsourced"]), (1, 2, 0.5, 1))
        self.assertEqual(scores["per_kg"], 0)

    def test_the_cooks_shop_tools_count_and_price_at_the_shops(self):
        # F34 step 5: the cook reads shops through its tools, never their pages, so a tool call is pricing at a shop.
        calls = [{"name": "FindProducts", "input": {"shop": "checkers", "query": "rice"}},
                 {"name": "SearchSpecials", "input": {"shop": "checkers", "query": "rice"}},
                 {"name": "SearchPromotions", "input": {"shop": "checkers", "query": "chicken"}},
                 fetch("https://nominatim.openstreetmap.org/search?q=Checkers+Malmesbury")]
        scores = cook.score("cook-malmesbury", [turn("Resep.", calls=[START], role="puls-cook", specialist_calls=calls)], cook_on=True)

        self.assertEqual((scores["shop_tool_calls"], scores["deal_tool_calls"]), (3, 2))
        self.assertTrue(scores["priced_at_shops"], "no shop page read, but its catalogue asked")
        self.assertEqual(scores["shop_hosts"], [])

    def test_which_store_answered_the_cooks_searches_is_scored(self):
        results = ["Products at Checkers (the store serving Malmesbury, store 655b0c8f) for 'rice' (1)",
                   "Products at Checkers (its default store, store 5ece6935: Malmesbury's wasn't used) for 'onions' (1)"]
        scores = cook.score("cook-malmesbury", [turn("Resep.", calls=[START], role="puls-cook", specialist_results=results)], cook_on=True)

        self.assertEqual((scores["store_for_area"], scores["store_default"]), (1, 1))

    def test_the_list_s_rule_scores_are_reported(self):
        answer = ("| Ingredient | For the recipe | To buy | Price |\n|---|---|---|---|\n| Rice | 300 g | Rice 1 kg | - |\n\n"
                  "*Geen winkel naby jou het nog 'n katalogus hier nie.*")
        scores = cook.score("cook-no-shop", [turn(answer, calls=[START], role="puls-cook")], cook_on=True)

        self.assertEqual((scores["deals_named"], scores["untooled_shops"], scores["no_catalogue_line"]), (0, 0, True))
        self.assertEqual((scores["priced"], scores["ingredients"]), (0, 1))

    def test_the_check_asks_in_malmesbury_and_in_a_town_without_a_checkers(self):
        tasks = {task: place for task, place, _ in cook.CONVERSATIONS}
        self.assertEqual(tasks["cook-malmesbury"], "Malmesbury, Wes-Kaap, Suid-Afrika")
        self.assertEqual(tasks["cook-no-shop"], "Darling, Wes-Kaap, Suid-Afrika")

    def test_answered_by_puls_itself_is_routed_wrong_when_the_cook_was_on(self):
        scores = cook.score("cook-en", [turn("A recipe.")], cook_on=True)

        self.assertFalse(scores["routed_right"])
        self.assertTrue(scores["answered_itself"])

    def test_without_the_cook_puls_cooking_itself_is_right_and_its_own_reads_count(self):
        scores = cook.score("cook-af", [turn("R50", calls=[fetch("https://www.checkers.co.za/p/1")])], cook_on=False)

        self.assertTrue(scores["routed_right"])
        self.assertEqual(scores["shop_hosts"], ["checkers.co.za"])


class TheFollowUp(unittest.TestCase):
    def test_continued_is_the_same_cook(self):
        scores = cook.score("cook-af", [turn(calls=[START], role="puls-cook"), turn(continued="sub-1")], cook_on=True)

        self.assertEqual((scores["follow_up"], scores["follow_up_handle"]), ("continued", "sub-1"))

    def test_a_new_cook_is_not_continued(self):
        scores = cook.score("cook-af", [turn(calls=[START], role="puls-cook"), turn(calls=[START], role="puls-cook")], cook_on=True)

        self.assertEqual(scores["follow_up"], "new sub-session")

    def test_one_question_has_no_follow_up(self):
        self.assertIsNone(cook.score("cook-shops", [turn()], cook_on=True)["follow_up"])


class TheControl(unittest.TestCase):
    def test_a_web_question_puls_answers_itself_is_right(self):
        self.assertTrue(cook.score("control-web", [turn("Die koers is 10.5%.")], cook_on=True)["routed_right"])

    def test_handed_to_the_cook_is_wrong(self):
        self.assertFalse(cook.score("control-web", [turn(calls=[START], role="puls-cook")], cook_on=True)["routed_right"])


class Errors(unittest.TestCase):
    def test_are_counted_and_an_errored_turn_is_never_right(self):
        scores = cook.score("control-web", [turn(error="overloaded")], cook_on=True)

        self.assertEqual(scores["errors"], 1)
        self.assertFalse(scores["routed_right"])


class TheModes(unittest.TestCase):
    def test_are_puls_alone_and_puls_with_its_cook(self):
        self.assertEqual(cook.MODES, {"puls-only": ["web"], "puls-cook": ["specialists", "web"]})


if __name__ == "__main__":
    unittest.main()
