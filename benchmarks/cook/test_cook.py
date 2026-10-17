"""The cook check's scoring (F31 step 7b): what Puls and its cook called, not only what they wrote, as numbers."""
import unittest

from benchmarks.cook import run_check as cook


def turn(answer="", calls=(), specialist_calls=(), role=None, continued=None, error=None):
    return {"answer": answer, "calls": list(calls), "specialist_calls": list(specialist_calls), "role": role,
            "continued": continued, "error": error, "seconds": 1.0}


def fetch(url):
    return {"name": "FetchUrl", "input": {"url": url}}


START = {"name": "StartSubSession", "input": {"personality": "puls-cook"}}


class AMealQuestion(unittest.TestCase):
    def test_handed_to_the_cook_is_routed_right_with_its_shops_and_rating(self):
        first = turn("Meel R22.99\nGeprys: 7 van 9 bestanddele", calls=[START], role="puls-cook",
                     specialist_calls=[fetch("https://www.checkers.co.za/p/1"), fetch("https://www.pnp.co.za/p/2"), fetch("https://www.bbc.co.uk/food")])
        scores = cook.score("cook-af", [first], cook_on=True)

        self.assertEqual(scores["role"], "puls-cook")
        self.assertTrue(scores["routed_right"])
        self.assertEqual(scores["shop_hosts"], ["checkers.co.za", "pnp.co.za"])
        self.assertFalse(scores["sources_local"], "a UK recipe site isn't South African")
        self.assertEqual((scores["priced"], scores["ingredients"], scores["priced_ratio"]), (7, 9, 0.78))

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
