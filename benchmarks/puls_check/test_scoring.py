"""The scoring every check shares (F31 step 7a): deterministic code over a transcript, never a model."""
import unittest

from benchmarks.puls_check import scoring


class Prices(unittest.TestCase):
    def test_rand_amounts_are_counted(self):
        self.assertEqual(scoring.rand_amounts("Meel R22.99, eiers R 45,00 en melk R18"), 3)

    def test_the_cooks_own_tally_is_its_rating(self):
        self.assertEqual(scoring.priced("...\nGeprys: 7 van 9 bestanddele"), (7, 9))

    def test_without_a_tally_the_table_rows_with_a_price_and_no_estimate_count(self):
        answer = "| Bestanddeel | Checkers |\n|---|---|\n| Meel | R22.99 |\n| Eiers | onbekend |\n| Melk | ±R18 |\n"
        self.assertEqual(scoring.priced(answer), (1, 3))

    def test_no_list_is_no_rating(self):
        self.assertEqual(scoring.priced("Net 'n resep, geen lys."), (None, None))

    def test_unconfirmed_marks(self):
        self.assertEqual(scoring.unconfirmed_marks("Prys onbekend; die ander is geskat uit die soekindeks."), 3)


if __name__ == "__main__":
    unittest.main()
