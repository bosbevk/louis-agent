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
        self.assertEqual(scoring.priced(answer), (1, 3, 0))

    def test_a_dash_is_a_shop_without_a_price(self):
        # The cook writes `-` for a price it couldn't find (Louis, review of PR #55: "make unknown --> -"). A row priced
        # with its link at one shop and `-` at another is priced; a row of dashes is listed, not priced, not guessed.
        answer = ("| Bestanddeel | Checkers | Shoprite | Beste prys |\n|---|---|---|---|\n"
                  "| Suurlemoene | [R12,99](https://www.checkers.co.za/p/1) | - | R12,99 Checkers |\n"
                  "| Koljander | - | - | - |\n")
        self.assertEqual(scoring.priced(answer), (1, 2, 0))

    def test_a_price_without_a_link_is_a_guess_not_priced(self):
        answer = "| Bestanddeel | Prys |\n|---|---|\n| Meel | R22.99 |\n| Eiers | R45.00 |\n| Melk | [R18](https://pnp.co.za/m) |\n"
        self.assertEqual(scoring.priced(answer), (1, 3, 2))

    # Review of PR #55: "any link in the row counts, even a recipe link, and reference-style links count as guesses". A
    # price is sourced only by a link to the shop whose column it's in (or, under a column naming no shop, to a shop).

    def test_a_recipe_link_is_no_source(self):
        answer = ("| Bestanddeel | Checkers |\n|---|---|\n"
                  "| Hoender | [R89,99](https://www.food24.com/recipes/hoender) |\n")
        self.assertEqual(scoring.priced(answer), (0, 1, 1))

    def test_another_shops_link_is_no_source_for_this_shops_price(self):
        answer = ("| Bestanddeel | Checkers | Pick n Pay |\n|---|---|---|\n"
                  "| Hoender | [R89,99](https://www.pnp.co.za/hoender) | - |\n"
                  "| Uie | - | [R24,99](https://www.pnp.co.za/uie) |\n")
        self.assertEqual(scoring.priced(answer), (1, 2, 1))

    def test_a_reference_style_link_is_its_url(self):
        answer = ("| Bestanddeel | Checkers |\n|---|---|\n| Hoender | [R89,99][1] |\n| Uie | [R24,99][uie] |\n\n"
                  "[1]: https://www.checkers.co.za/p/hoender\n[uie]: https://www.checkers.co.za/p/uie \"Uie\"\n")
        self.assertEqual(scoring.priced(answer), (2, 2, 0))

    def test_a_shops_own_subdomain_is_the_shop(self):
        answer = "| Bestanddeel | Woolworths |\n|---|---|\n| Melk | [R32,99](https://shop.woolworths.co.za/melk) |\n"
        self.assertEqual(scoring.priced(answer), (1, 1, 0))

    def test_a_host_that_only_ends_like_a_shop_is_not_the_shop(self):
        answer = "| Bestanddeel | Checkers |\n|---|---|\n| Melk | [R32,99](https://notcheckers.co.za/melk) |\n"
        self.assertEqual(scoring.priced(answer), (0, 1, 1))

    def test_the_cooks_own_tally_is_a_claim_not_a_rating(self):
        # "Geprys: 11 van 11" under a list priced from memory: the rows say what was sourced.
        answer = "| Bestanddeel | Prys |\n|---|---|\n| Meel | R22.99 |\n\nGeprys: 11 van 11 bestanddele"
        self.assertEqual(scoring.priced(answer), (0, 1, 1))

    def test_only_the_shopping_lists_ingredient_rows_count(self):
        # The cook's real answer (F31 step 11b): the list with its bold totals, then the branches and what's unsure.
        answer = ("| Bestanddeel | Koop | Checkers |\n|---|---|---|\n| Boerewors | 1 kg | unknown |\n"
                  "| Uie | 1 kg | [R24.99](https://www.checkers.co.za/uie) |\n| **Totaal** | | **R24.99** |\n\n"
                  "| Winkel | Tak | Webwerf |\n|---|---|---|\n| Checkers | Andringa Street | [checkers.co.za](https://www.checkers.co.za/) |\n\n"
                  "| Wat | Hoekom |\n|---|---|\n| Alle pryse | R0 gevind |\n")
        self.assertEqual(scoring.priced(answer), (1, 2, 0))

    def test_a_tally_alone_rates_nothing(self):
        self.assertEqual(scoring.priced("...\nGeprys: 7 van 9 bestanddele"), (None, None, None))

    def test_no_list_is_no_rating(self):
        self.assertEqual(scoring.priced("Net 'n resep, geen lys."), (None, None, None))

    def test_unconfirmed_marks(self):
        self.assertEqual(scoring.unconfirmed_marks("Prys onbekend; die ander is geskat uit die soekindeks."), 3)


if __name__ == "__main__":
    unittest.main()
