"""Every check's manual (F31 step 10b) names every score its check reports, so a new score can't ship unexplained."""
import os
import re
import unittest

from benchmarks.cook import run_check as cook
from benchmarks.routing import run_check as routing

HERE = os.path.dirname(os.path.abspath(__file__))


def manual(check):
    with open(os.path.join(HERE, check, "HANDLEIDING.md"), encoding="utf-8") as file:
        return file.read()


def named(text):
    return set(re.findall(r"`([a-z_]+)`", text))


TURN = {"answer": "", "calls": [], "specialist_calls": [], "role": None, "continued": None, "error": None}


class EveryScore(unittest.TestCase):
    def test_of_the_cook_check_is_in_its_manual(self):
        keys = cook.score("cook-af", [TURN, TURN], cook_on=True).keys()
        self.assertEqual(set(keys) - named(manual("cook")), set())

    def test_of_the_routing_check_is_in_its_manual(self):
        keys = routing.score("cook", TURN, delegating=True).keys()
        self.assertEqual(set(keys) - named(manual("routing")), set())

    def test_of_the_stub_is_in_its_manual(self):
        self.assertEqual({"routed_right", "turns"} - named(manual("stub")), set())


class EveryCheck(unittest.TestCase):
    def test_has_a_manual_beside_its_script(self):
        for check in ("stub", "cook", "routing"):
            self.assertTrue(manual(check).startswith("# "), check)


if __name__ == "__main__":
    unittest.main()
