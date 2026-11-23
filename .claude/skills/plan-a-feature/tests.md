# Test jobs for plan-a-feature

Three jobs to try this skill on, in a fresh session that did not write it (`/write-a-skill`, step 7). Each ask is
said the way Louis says it. The skill passes a job when the session loads it from the ask alone, everything in
*Must happen* happens, and nothing in *Must not* does.

| # | Where, and what Louis says | Must happen | Must not |
|---|---|---|---|
| 1 | A clone that is a day behind `main`, which has taken the next `F` number since. He says: "plan a feature so the guide can answer on WhatsApp" | `origin/main` is fetched, and the id is the next free one in the TODO and the work log there. The research comes first: the code that is there, WhatsApp's own docs read on the web that day with links, and how two or three others built it. Each open choice goes to him on a card before the plan is written. The plan has its overview and four pictures, a namespace map, steps of one commit each, and a *Done when* | An id another plan holds. A fact about WhatsApp from memory. A step started before he approves the plan |
| 2 | A plan in draft has fourteen steps. He says: "write it up" | It is split into a parent with dotted children, each with its own plan, branch, pull request, done-when and write-up. Only the parent and the first child are written now | One plan of fourteen steps. Every child's plan written at once |
| 3 | An approved plan, three steps in. He says: "make it only the guide, the cook moves to its own repo" | The change is added under *Approved during the build*, with the date and his words. The steps it removes are named there | The plan's approved text rewritten. A step built that the change removed |
| 4 | A plan that adds a Razor library with three pages and five components, its design doc in the last step. He says: "plan the run pages" | Each new folder's files are counted against the folder rule's six, and a folder that would pass it is split in the plan. The plan starts the new project's design doc with its first component, since the docs' rules ask for one from then | A step that adds a seventh file to a folder. The design doc left to the close while pages land before it |
