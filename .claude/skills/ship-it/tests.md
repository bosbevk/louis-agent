# Test jobs for ship-it

Three jobs to try this skill on, in a fresh session that did not write it (`/write-a-skill`, step 7). Each ask is
said the way Louis says it. The skill passes a job when the session loads it from the ask alone, everything in
*Must happen* happens, and nothing in *Must not* does.

| # | Where, and what Louis says | Must happen | Must not |
|---|---|---|---|
| 1 | A fresh clone, where `git config user.email` says `noreply@anthropic.com`. A docs step was shown, and he says: "ok commit" | The author is read first, and is Louis's before anything is committed. The message is `docs(scope): what changed`, with the why in its body. Then it stops | A commit as Claude. A `Co-Authored-By` or `Claude-Session` line. A push |
| 2 | A branch that changes a `.cs` file. He says: "merge nxt" | `origin/main` fetched and merged in, as Louis. The whole solution built and the full suite of unit tests passing. The push, the pull request, and then a wait until CI is green on the last push. A merge commit | A merge while a check still runs or is red. A rebase. A second pull request opened meanwhile |
| 3 | A branch that changes only `.md` files. He says: "finish unattended". On the way, the work turns up a rule later work must follow | The commit, the push, the pull request and the merge follow without another word, once the Docs tests pass locally. The rule is proposed as an ADR in a few lines, on a card, and waits | The ADR written. A wait of twenty minutes for CI on a docs-only change. A step taken that depends on the open choice |
