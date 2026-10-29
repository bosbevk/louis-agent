# Lessons from the fixes

> **Answers:** what each fix taught, in one place, now that the fix docs are folded (CL24, 2026-10-29) · **Written by**
> Claude, for Louis to read

Each fix's symptom and cause stay in its row of the [fixes table](../fixes/README.md); the whole doc as it was is at
the tag [`pre-cl24`](https://github.com/bosbevk/louis-agent/tree/pre-cl24/docs/fixes). Here is what each one taught,
with its skill in the [goal](../GOAL.md)'s map. The ones with a write-up of their own skill are told at more length
there: FX1 and FX5 to FX7 in [13](13-cost-and-token-economics.md), FX10 to FX14 in
[10](10-multi-agent-orchestration.md), FX18 to FX20 in [15](15-safety-and-security.md), FX25 in
[12](12-evaluation.md).

## FX1 · Keep the tools on the request after the 10-round limit

**Preserved thinking**: newer Claude models tie their thinking blocks to the exact conversation (system prompt, tools,
earlier messages), so any layer that edits a request in flight, even a library's, must keep it append-only. Skill map #2
(reasoning and extended thinking) and #14 (reliability). The same rule is behind [FX4](../fixes/FX04-append-only-history.md).

## FX2 · The usage probe says which model to pull on an Ollama 404

An error a person will meet should say what to do next, not only what failed (skill map #14, reliability).

## FX3 · The Ollama container starts and pulls the profile's model

A shell script run inside a Linux container must have LF endings however it is checked out on Windows; `.gitattributes`
protects new checkouts, not old ones (skill map #18, testing and shipping).

## FX5 · Requests outside a usage scope are written to the ledger again

`?.` short-circuits the rest of the expression, arguments included, so a side effect inside the argument (writing,
counting, logging) disappears when the target is null. Keep side effects out of a null-conditional call. And
recording must never depend on optional context: the ledger is the source of truth, and the scope is only a view of it.
The tests that caught it were written for F1, which is why every step keeps its tests (skill map #17, observability;
#18, testing and shipping).

## FX6 · A turn that fails still reports what it used

A total is only as good as its worst path: failure paths must report what they used, or every view built on the
happy path drifts from the ledger, quietly, exactly when something went wrong (skill map #17, observability; #13,
cost).

## FX7 · A timed-out request no longer ends the orchestrator's run

Cancellation is not one thing: "the user stopped" and "something timed out" share an exception type in .NET, so tell
them apart by the token that was cancelled, never by the type alone (skill map #14, reliability: timeouts in long
tasks; #18, testing and shipping).

## FX8 · A FIX-RESULT with quotes in its summary is still read

A machine-readable line written by a model is still model output: parse it tolerantly, and never answer a parse
failure with a request the model can't act on differently, since every retry is paid for (skill map #5, structured
outputs, which F8 addresses with results as tool calls; #13, cost).

## FX9 · A request cancelled without streaming is still written to the ledger

A ledger that only records successes undercounts exactly the requests that went wrong, which are the ones worth
seeing. Skill 17 (observability) in the [goal](../GOAL.md).

## FX10 · A sub-session call without `agent` uses the only peer

A tool's schema is the contract, and the description is only advice. When the description says a value can be left
out, the schema has to agree, or the model gets refused for following the advice. Skill 4 (tool design) in the
[goal](../GOAL.md).

## FX11 · A sub-session that ends without an answer is asked once more, in the same session

Continue the context you already paid for; don't start over. A retry in a fresh session re-buys the prefix and redoes
the work. A follow-up in the same session reads both from the cache. The same idea is behind prompt caching (F4) and
the orchestrator's continue loop. Skill 10 (multi-agent orchestration) and skill 13 (cost and token economics) in the
[goal](../GOAL.md).

## FX12 · `.env` templates (`.env.example`) can be read

A guardrail that blocks too much has a cost too: the agent works around it, guesses, or gives up. Block by what a
file holds, not by what its name looks like, and test both the blocked and the allowed side. Skill 15 (safety and
security) in the [goal](../GOAL.md).

## FX13 · Counting goes to the analyst, and the reader hands it back

Routing is a prompt, so it can fail, and the cost of a wrong route depends on what the wrong specialist does next. Tell
each role what is *not* its job and what to do instead, so a wrong route fails cheaply. Skill 10 (multi-agent
orchestration) and skill 3 (prompt design) in the [goal](../GOAL.md).

## FX14 · A sub-session's prompt has a budget

Limit the tail in code. A prompt can make a failure less likely, but only a limit decides what a failure costs. Context
grows with every tool call, and long-context pricing turns that growth into a cliff, so the limit belongs on the
context. Skill 13 (cost and token economics) and skill 14 (reliability) in the [goal](../GOAL.md).

## FX15 · A test passed only on a machine with `config/.env`

A test that reads from the machine instead of from what it was given passes where it was written and fails where it is
first run elsewhere. CI's first job was to find the hidden local state, which is exactly why it exists. Skill 12
(evaluation) in the [goal](../GOAL.md): a check is only a check if it runs the same everywhere.

## FX16 · A quote that is only a price was "Found" for any product at that price

**A check is only as strong as what it binds** (skill 12, evaluation; skill 15, guardrails in code): "the words are on
the page" proves the words, and a price alone is a word every product at that price shares. A verifier has to tie the
claim to its subject (the price to its product) or say it can't, else a *pass* overstates what was checked, and every
score built on it does too.

## FX17 · The cook wrote "Serves" in Afrikaans, "Geprys" in English, a count before a photo, and a follow-up's card said "sub-1"

A label the model is meant to translate has to be shown translated: one literal form is copied as it stands, whatever
the task's language (skill 3, *Prompt design*). And a card that names who it's waiting on needs that name from the one
who's working, not only from the call that sent the work: a call can be a follow-up that never names it (skill 10,
*Multi-agent orchestration*).

## FX18 · Without a key, the API and the MCP server over HTTP serve anyone who can reach them

Deny by default (OWASP Top 10:2021, A01): fail closed at start-up rather than at the first request, with one pure rule
tested without a host. A warning that a door is open is not a guard: the operator who never reads the log never
closes it, so the host has to refuse, and the one who wants it open says so in a setting that is never set in a
compose file. And unset and set-but-empty are different values in the environment: a fail-closed rule has to treat
them the same everywhere a key is read, or a blank line in an env file opens the door (the review's catch). Skill map
#15 (safety and security).

## FX19 · A value from the model can reach git as an option, and some options run commands

Argument injection (OWASP LLM05:2025; CWE-88): a list stops splitting, not options; `--end-of-options`, from git's
`gitcli` docs, and an allow-list rather than a deny-list. And a test for a guard has to fail for the hole, not for a
side effect: the branch name `x --upload-pack=…` is refused by git whatever we do, so it proves nothing on its own; the
test that proved this fix was the file `git show --output` wrote. Skill map #15 (safety and security).

## FX20 · A skill file the agent writes is loaded at once, for every later session, with no person in between

Memory poisoning (OWASP Top 10 for Agentic Applications, ASI06): what the agent writes for later sessions is untrusted
until a person approves it, however reasonable it reads, because the model that wrote it may have been reading an
attacker's page. The gate costs nothing new: agent-built tools already had it, and the engine's "files I wrote" list
already kept a file alive for one session; the fix was to stop the file reaching every other session on its own.
Skill map #15 (safety and security) and #16 (human in the loop).

## FX21 · The browser reads shops' pages for a user without saying what it is

A disguise is a decision about the product, not a rendering fix: whether the agent tells sites what it is decides
what a retailer finds in their logs and whether the sold endpoint can say it never pretended. The principles that open
every prompt bind the code around the model too.

## FX22 · The recipe's image showed as a link: an image renders when a tool gave it

A guard that names hosts guards against yesterday's hosts. What a page should be allowed to load is what the turn's own
tools returned, which a model can repeat but not invent: the same shape as ADR 0004's arguments as data, applied to
output.

## FX24 · The cook's recipe photo and its estimates: the grader, the chip, the box and the prompt

When a grader fails a good answer, check the grader before the model: open the page yourself. `recipe-image` v1
failed answers for icons on pages the cook only passed through, and the chip counted estimates as unpriced, so both
measured the wrong thing (skill 12, evaluation).

## FX25 · The eval runner gets only the settings it reads, not the API's secrets

**Least privilege for a child process** (the goal's *Security and guardrails*): what a process can read is part of its interface. A child inherits
the parent's environment unless told otherwise, so "it only reads four settings" is not the same as "it can only see four settings". The allowlist
makes the runner's inputs explicit, which is also documentation: the list *is* what the runner depends on. The paste's other three points (the
runner as its own job with its own identity, sign-in with roles and an audit log, and how long transcripts are kept) are Phase 9's.

---
[Learning](README.md) · [Fixes](../fixes/README.md) · [Docs index](../README.md)
