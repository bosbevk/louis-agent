"""The routing check's scoring (F31 step 8): does Puls hand each kind of question to the right place? Only cooking has
a role that's on (F29), so every other question is right when Puls answers it itself."""
import unittest

from benchmarks.routing import run_check as routing


def turn(role=None, calls=(), specialist_calls=(), error=None, answer="Antwoord."):
    return {"answer": answer, "calls": list(calls), "specialist_calls": list(specialist_calls), "role": role,
            "continued": None, "error": error}


class TheExpectedRole(unittest.TestCase):
    def test_is_the_cook_for_a_meal_and_nobody_otherwise(self):
        self.assertEqual(routing.expected_role("cook-2"), "puls-cook")
        for task in ("web", "files-2", "git", "numbers", "chat-2"):
            self.assertIsNone(routing.expected_role(task), task)

    def test_every_question_has_a_kind_the_check_knows(self):
        self.assertEqual(len(routing.QUESTIONS), 12)
        for task, _ in routing.QUESTIONS:
            self.assertIn(task.split("-")[0], routing.KINDS, task)


class Delegated(unittest.TestCase):
    def test_a_meal_handed_to_the_cook_is_right(self):
        scores = routing.score("cook", turn(role="puls-cook", calls=[{"name": "StartSubSession", "input": {}}]), delegating=True)
        self.assertTrue(scores["routed_right"])
        self.assertEqual(scores["delegations"], 1)

    def test_a_meal_puls_cooked_itself_is_wrong(self):
        self.assertFalse(routing.score("cook", turn(), delegating=True)["routed_right"])

    def test_a_web_question_puls_answered_itself_is_right(self):
        self.assertTrue(routing.score("web", turn(), delegating=True)["routed_right"])

    def test_a_web_question_handed_away_is_wrong(self):
        self.assertFalse(routing.score("web-2", turn(role="puls-cook"), delegating=True)["routed_right"])


class TheBaseline(unittest.TestCase):
    def test_cant_delegate_so_answering_itself_is_always_right(self):
        self.assertTrue(routing.score("cook", turn(), delegating=False)["routed_right"])

    def test_is_every_available_toolset_but_the_delegating_ones(self):
        available = ["files", "git", "web", "specialists", "minions", "skills"]
        self.assertEqual(routing.baseline(available), ["files", "git", "web", "skills"])

    def test_an_error_is_never_right(self):
        scores = routing.score("chat", turn(error="overloaded"), delegating=False)
        self.assertFalse(scores["routed_right"])
        self.assertEqual(scores["errors"], 1)


class TheModes(unittest.TestCase):
    def test_delegated_is_puls_as_built(self):
        self.assertEqual(routing.DELEGATED, ["skills", "specialists", "web"])


if __name__ == "__main__":
    unittest.main()
