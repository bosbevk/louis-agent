"""The stream reader against the recorded turn (F31 step 7a): it must read what the web app's TurnStream reads, the
counts in fixtures/session-stream.expected.json, which the C# test holds TurnStream to as well."""
import json
import os
import unittest

from benchmarks.puls_check import stream

FIXTURES = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "fixtures")


def recorded():
    with open(os.path.join(FIXTURES, "session-stream.sse"), encoding="utf-8") as file:
        return list(stream.parse_sse(file))


class TheRecordedTurn(unittest.TestCase):
    def setUp(self):
        with open(os.path.join(FIXTURES, "session-stream.expected.json"), encoding="utf-8") as file:
            self.expected = json.load(file)
        self.turn = stream.read_turn(recorded())

    def test_puls_calls_and_the_role_it_handed_to(self):
        self.assertEqual([c["name"] for c in self.turn["calls"]], self.expected["puls_tools"])
        self.assertEqual(self.turn["role"], self.expected["role"])

    def test_the_specialists_calls_from_its_nested_events(self):
        self.assertEqual([c["name"] for c in self.turn["specialist_calls"]], self.expected["specialist_tools"])

    def test_the_hosts_read(self):
        self.assertEqual(stream.hosts(self.turn["calls"] + self.turn["specialist_calls"]), self.expected["hosts"])

    def test_the_answer(self):
        self.assertTrue(self.turn["answer"].startswith(self.expected["answer_starts"]))
        self.assertEqual(len(self.turn["answer"]), self.expected["answer_length"])
        self.assertEqual(self.turn["error"], self.expected["error"])


class TheSitesRead(unittest.TestCase):
    # Louis, on run d94597a51d4d's sources ring: "who is the elsewhere?" (OpenStreetMap, the cook's branch lookup), and
    # no SA shop at all, though every price came from Checkers: a shop search reads its shop's catalogue too.
    def test_a_shop_search_is_a_read_of_its_shop(self):
        calls = [{"name": "FindProducts", "input": {"shop": "checkers", "query": "rice"}},
                 {"name": "SearchSpecials", "input": {"shop": "Checkers", "query": "chicken"}},
                 {"name": "FetchUrl", "input": {"url": "https://nominatim.openstreetmap.org/search?q=Checkers+Malmesbury"}},
                 {"name": "FindProducts", "input": {"shop": "atlantis", "query": "rice"}}]

        self.assertEqual(stream.hosts(calls), ["checkers.co.za", "nominatim.openstreetmap.org"])


class EveryEvent(unittest.TestCase):
    def test_is_kept_for_the_portal(self):
        events = recorded()
        self.assertGreater(len(events), 1000)
        self.assertEqual(events[0][0], "message_start")
        self.assertTrue(all(isinstance(data, dict) for _, data in events))

    def test_an_error_event_is_the_turns_error(self):
        turn = stream.read_turn([("error", {"type": "error", "error": {"type": "api_error", "message": "overloaded"}})])
        self.assertIn("overloaded", turn["error"])

    def test_a_calls_role_is_its_role_and_a_recorded_calls_personality_too(self):
        # CL14: StartSubSession's argument is "role"; the fixture, recorded before CL14, says "personality" and still reads.
        call = {"type": "content_block_start", "content_block": {"type": "tool_use", "name": "StartSubSession", "input": {"role": "puls-cook"}}}
        old = {"type": "content_block_start", "content_block": {"type": "tool_use", "name": "StartSubSession", "input": {"personality": "puls-cook"}}}
        self.assertEqual(stream.read_turn([("content_block_start", call)])["role"], "puls-cook")
        self.assertEqual(stream.read_turn([("content_block_start", old)])["role"], "puls-cook")

    def test_the_tools_results_are_kept_puls_s_and_the_specialists(self):
        # F34: a shop tool's result names the store that answered; the check counts it, so the results are kept.
        own = {"type": "content_block_start", "content_block": {"type": "tool_result", "tool_use_id": "t1", "content": "Saturday 17 October"}}
        inner = {"event": "content_block_start", "data": {"type": "content_block_start", "content_block": {
            "type": "tool_result", "tool_use_id": "t2", "content": "Products at Checkers (the store serving Malmesbury, store 655b0c8f) for 'rice' (1)"}}}
        turn = stream.read_turn([("content_block_start", own), ("sub_session", inner)])
        self.assertEqual(turn["results"], ["Saturday 17 October"])
        self.assertEqual(turn["specialist_results"], ["Products at Checkers (the store serving Malmesbury, store 655b0c8f) for 'rice' (1)"])

    def test_a_continue_is_a_delegation_to_the_same_handle(self):
        start = {"type": "content_block_start", "content_block": {"type": "tool_use", "id": "t1", "name": "ContinueSubSession", "input": {"handle": "sub-1"}}}
        turn = stream.read_turn([("content_block_start", start)])
        self.assertEqual(turn["continued"], "sub-1")
        self.assertIsNone(turn["role"])


if __name__ == "__main__":
    unittest.main()
