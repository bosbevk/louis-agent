# Test jobs for do-a-step

Three jobs to try this skill on, in a fresh session that did not write it (`/write-a-skill`, step 7). Each ask is
said the way Louis says it. The skill passes a job when the session loads it from the ask alone, everything in
*Must happen* happens, and nothing in *Must not* does.

| # | Where, and what Louis says | Must happen | Must not |
|---|---|---|---|
| 1 | A plan's step adds a refusal to a method. He says: "nxt" | The test for the refusal is written first, run alone, and seen failing on its assertion. Then the code, then the project's whole suite. The step is shown with the failing output | Code before the test. A test that fails only because it doesn't compile. A commit before his ok |
| 2 | A plan's step moves a long method's body into a class of its own, with no change in what it does. He says: "continue" | No new test is written for the move. The project's tests run before and after, unchanged, and pass. The step is shown naming the tests that hold the behaviour, and saying no new one was written | A test written only to have one. A test changed to make the move pass. The step shown as "tested" with no tests named |
| 3 | The same refactor turns up a check the old code didn't make. He says: "nxt" | The new check is put to him as a finding, or, on his word, gets its own test first, seen failing | The new check slipped in under "a refactor needs no new test" |
