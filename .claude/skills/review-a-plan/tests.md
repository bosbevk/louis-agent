# Test jobs for review-a-plan

Three jobs to try this skill on, in a fresh session that did not write it (`/write-a-skill`, step 7). Each ask is
said the way Louis says it. The skill passes a job when the session loads it from the ask alone, everything in
*Must happen* happens, and nothing in *Must not* does.

| # | Where, and what Louis says | Must happen | Must not |
|---|---|---|---|
| 1 | The thread that just wrote a plan. He says: "now review it" | Before anything else, it says the plan is its own and asks him for a fresh thread to review it. If he says to go on here, the review's first paragraph says so, and the pre-mortem is run with extra care | A review started in silence. A review that doesn't say whose plan it is |
| 2 | A fresh thread. He says: "proofread the F168 plan" | The plan, its pictures, his words in its *Context* and his picks are read. Every name the plan calls existing is opened in `origin/main` after a fetch. The findings go in a review file beside the plan in the project's files, under *A. Out of step*, each with where it is and the fix | An edit to the plan. A name called existing that was not opened. A review file in the repo |
| 3 | A fresh thread. He says: "what is this plan missing?" | The main case is walked as the person it is for, then the concerns that cross every plan, then the pre-mortem. A fact about a platform or a package is read in its own docs on the web that day, with the link. Each gap has three ways and a recommendation. The gap that changes the most goes to him first, on its own card | A gap closed in the review. All the gaps put to him at once. A platform's limit stated from memory |
