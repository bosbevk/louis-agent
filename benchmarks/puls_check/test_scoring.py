"""The scoring every check shares (F31 step 7a): deterministic code over a transcript, never a model."""
import unittest

from benchmarks.puls_check import scoring


class Prices(unittest.TestCase):
    def test_rand_amounts_are_counted(self):
        self.assertEqual(scoring.rand_amounts("Meel R22.99, eiers R 45,00 en melk R18"), 3)

    # F31 step 11b (Louis): "it was guessed; it should not count as answered. The idea is to get an actual answer." A
    # price counts only with the link it came from; a rand amount without one is a guess, counted apart.

    def test_a_price_with_its_link_is_priced(self):
        answer = ("| Bestanddeel | Checkers |\n|---|---|\n| Meel | [R22.99](https://www.checkers.co.za/p/1) |\n"
                  "| Eiers | onbekend |\n| Melk | ±R18 |\n")
        self.assertEqual(scoring.priced(answer), (1, 3, 0, 0))

    def test_a_dash_is_a_shop_without_a_price(self):
        # The cook writes `-` for a price it couldn't find (Louis, review of PR #55: "make unknown --> -"). A row priced
        # with its link at one shop and `-` at another is priced; a row of dashes is listed, not priced, not guessed.
        answer = ("| Bestanddeel | Checkers | Shoprite | Beste prys |\n|---|---|---|---|\n"
                  "| Suurlemoene | [R12,99](https://www.checkers.co.za/p/1) | - | R12,99 Checkers |\n"
                  "| Koljander | - | - | - |\n")
        self.assertEqual(scoring.priced(answer), (1, 2, 0, 0))

    def test_a_price_without_a_link_is_a_guess_not_priced(self):
        answer = "| Bestanddeel | Prys |\n|---|---|\n| Meel | R22.99 |\n| Eiers | R45.00 |\n| Melk | [R18](https://pnp.co.za/m) |\n"
        self.assertEqual(scoring.priced(answer), (1, 3, 2, 0))

    # Review of PR #55: "any link in the row counts, even a recipe link, and reference-style links count as guesses". A
    # price is sourced only by a link to the shop whose column it's in (or, under a column naming no shop, to a shop).

    def test_a_recipe_link_is_no_source(self):
        answer = ("| Bestanddeel | Checkers |\n|---|---|\n"
                  "| Hoender | [R89,99](https://www.food24.com/recipes/hoender) |\n")
        self.assertEqual(scoring.priced(answer), (0, 1, 1, 0))

    def test_another_shops_link_is_no_source_for_this_shops_price(self):
        answer = ("| Bestanddeel | Checkers | Pick n Pay |\n|---|---|---|\n"
                  "| Hoender | [R89,99](https://www.pnp.co.za/hoender) | - |\n"
                  "| Uie | - | [R24,99](https://www.pnp.co.za/uie) |\n")
        self.assertEqual(scoring.priced(answer), (1, 2, 1, 0))

    def test_a_reference_style_link_is_its_url(self):
        answer = ("| Bestanddeel | Checkers |\n|---|---|\n| Hoender | [R89,99][1] |\n| Uie | [R24,99][uie] |\n\n"
                  "[1]: https://www.checkers.co.za/p/hoender\n[uie]: https://www.checkers.co.za/p/uie \"Uie\"\n")
        self.assertEqual(scoring.priced(answer), (2, 2, 0, 0))

    def test_a_shops_own_subdomain_is_the_shop(self):
        answer = "| Bestanddeel | Woolworths |\n|---|---|\n| Melk | [R32,99](https://shop.woolworths.co.za/melk) |\n"
        self.assertEqual(scoring.priced(answer), (1, 1, 0, 0))

    def test_a_host_that_only_ends_like_a_shop_is_not_the_shop(self):
        answer = "| Bestanddeel | Checkers |\n|---|---|\n| Melk | [R32,99](https://notcheckers.co.za/melk) |\n"
        self.assertEqual(scoring.priced(answer), (0, 1, 1, 0))

    def test_the_cooks_own_tally_is_a_claim_not_a_rating(self):
        # "Geprys: 11 van 11" under a list priced from memory: the rows say what was sourced.
        answer = "| Bestanddeel | Prys |\n|---|---|\n| Meel | R22.99 |\n\nGeprys: 11 van 11 bestanddele"
        self.assertEqual(scoring.priced(answer), (0, 1, 1, 0))

    def test_only_the_shopping_lists_ingredient_rows_count(self):
        # The cook's real answer (F31 step 11b): the list with its bold totals, then the branches and what's unsure.
        answer = ("| Bestanddeel | Koop | Checkers |\n|---|---|---|\n| Boerewors | 1 kg | unknown |\n"
                  "| Uie | 1 kg | [R24.99](https://www.checkers.co.za/uie) |\n| **Totaal** | | **R24.99** |\n\n"
                  "| Winkel | Tak | Webwerf |\n|---|---|---|\n| Checkers | Andringa Street | [checkers.co.za](https://www.checkers.co.za/) |\n\n"
                  "| Wat | Hoekom |\n|---|---|\n| Alle pryse | R0 gevind |\n")
        self.assertEqual(scoring.priced(answer), (1, 2, 0, 0))

    def test_a_tally_alone_rates_nothing(self):
        self.assertEqual(scoring.priced("...\nGeprys: 7 van 9 bestanddele"), (None, None, None, None))

    def test_no_list_is_no_rating(self):
        self.assertEqual(scoring.priced("Net 'n resep, geen lys."), (None, None, None, None))

    # Review of PR #58: the cook priced packs sold by weight at their price per kg (run fc7f8ecfdc87), and the scorer
    # counted them as priced, since each had its link. A price per kg is no pack's price: counted apart, as per kg,
    # neither priced nor guessed. A pack sold by weight costs a range (FindProducts: lightest to heaviest pack).
    HEADER_EN = "| Ingredient | Recipe needs | Buy | Checkers | Shoprite | Pick n Pay | Best price |\n|---|---|---|---|---|---|---|\n"
    HEADER_AF = "| Bestanddeel | Hoeveel | Koop | Checkers | Shoprite | Goedkoopste |\n|---|---|---|---|---|---|\n"
    THIGHS = "https://www.checkers.co.za/product/farmers-choice-chicken-8-piece-thighs-per-kg-10732975KG"

    def test_a_price_that_says_per_kg_is_no_packs_price(self):
        # cook-en's row, as the run wrote it.
        answer = self.HEADER_EN + f"| Chicken thighs (Farmer's Choice, 8 piece) | 700 g | 1 kg | [R99,99/kg]({self.THIGHS}) | - | - | R99,99 Checkers |\n"
        self.assertEqual(scoring.priced(answer), (0, 1, 0, 1))

    def test_one_price_for_a_product_sold_by_weight_is_its_price_per_kg(self):
        # cook-shops' row: a 1–1,2 kg pack at R99,99, the price per kg (Checkers' link ends in the unit, KG).
        answer = self.HEADER_AF + f"| Hoender, bo-been dye en boude | 1 kg | 1–1,2 kg pak | [R99,99]({self.THIGHS}) | - | R99,99 Checkers |\n"
        self.assertEqual(scoring.priced(answer), (0, 1, 0, 1))

    def test_a_range_for_a_product_sold_by_weight_is_its_packs_price(self):
        answer = (self.HEADER_AF + f"| Hoender | 1 kg | 1–1,2 kg pak | [R99,99–R119,99]({self.THIGHS}) | - | R119,99 Checkers |\n"
                  f"| Hoender | 1 kg | 1–1,2 kg pak | [R99,99 – 119,99]({self.THIGHS}) | - | R119,99 Checkers |\n")
        self.assertEqual(scoring.priced(answer), (2, 2, 0, 0))

    def test_one_price_for_a_product_sold_each_is_its_packs_price(self):
        answer = self.HEADER_AF + "| Uie | 2 | 1 kg sak | [R34,99](https://www.checkers.co.za/product/onions-1kg-10135699EA) | - | R34,99 Checkers |\n"
        self.assertEqual(scoring.priced(answer), (1, 1, 0, 0))

    # F34 step 5: the cook prices only shops it has a tool for (Louis), names a deal beside the pack's price, and says
    # so when no shop near the user has a catalogue. The rows are shaped as the cook's prompt asks.
    ONE_SHOP = "| Ingredient | For the recipe | To buy | Checkers |\n|---|---|---|---|\n"

    def test_a_deal_named_beside_a_price_is_counted(self):
        answer = (self.ONE_SHOP + "| Rice | 300 g | Spekko Rice 1 kg | [R24,99](https://www.checkers.co.za/p/1) · BUY 2 FOR R45 |\n"
                  "| Chicken | 1 kg | Farmer's Choice 16-piece | [R135,98–R151,98](https://www.checkers.co.za/p/2KG) · SAVE |\n"
                  "| Onions | 2 | Onions 1 kg | [R34,99](https://www.checkers.co.za/p/3) |\n")
        self.assertEqual(scoring.deals_named(answer), 2)

    def test_no_deal_is_none_named(self):
        self.assertEqual(scoring.deals_named(self.ONE_SHOP + "| Onions | 2 | Onions 1 kg | [R34,99](https://www.checkers.co.za/p/3) |\n"), 0)

    def test_a_shop_without_a_tool_in_the_list_is_counted(self):
        # Checkers has a tool (FindProducts); Pick n Pay and Shoprite don't: their columns break the rule.
        answer = ("| Ingredient | For the recipe | To buy | Checkers | Pick n Pay | Shoprite | Best price |\n|---|---|---|---|---|---|---|\n"
                  "| Onions | 2 | Onions 1 kg | [R34,99](https://www.checkers.co.za/p/3) | - | - | R34,99 Checkers |\n")
        self.assertEqual(scoring.untooled_shops(answer), 2)
        self.assertEqual(scoring.untooled_shops(self.ONE_SHOP + "| Onions | 2 | Onions 1 kg | - |\n"), 0)

    def test_the_no_catalogue_line_is_found_in_either_language(self):
        self.assertTrue(scoring.no_catalogue_line("...\n_No shop near you has a catalogue here yet._"))
        self.assertTrue(scoring.no_catalogue_line("...\n*Geen winkel naby jou het nog 'n katalogus hier nie.*"))
        self.assertFalse(scoring.no_catalogue_line("Geprys: 3 van 3 bestanddele"))

    def test_which_store_answered_is_counted_from_the_tools_headers(self):
        # Louis: "how can we verify that the product comes from the malmesbury store?" The tool names the store that
        # answered, checked against Checkers' own storeId; a search the town's store didn't answer says so.
        results = ["Products at Checkers (the store serving Malmesbury, store 655b0c8f) for 'rice' (1), from the shop's own catalogue",
                   "Specials (multi-buys) at Checkers (the store serving Malmesbury, store 655b0c8f) for 'coffee' (2)",
                   "Products at Checkers (its default store, store 5ece6935: Malmesbury's wasn't used) for 'onions' (1)",
                   "Products at Checkers (its default store: no area shared) for 'eggs' (3)",
                   "Saturday 17 October 2026"]
        self.assertEqual((scoring.store_for_area(results), scoring.store_default(results)), (2, 2))

    def test_unconfirmed_marks(self):
        self.assertEqual(scoring.unconfirmed_marks("Prys onbekend; die ander is geskat uit die soekindeks."), 3)


if __name__ == "__main__":
    unittest.main()
