# Specialists Skills

How to hand a piece of work to a specialist agent with `StartSubSession`. A sub-session runs on another louis-agent
(a peer in `AGENT_PEERS`) with only the toolsets the task needs; you get back its final answer and a usage line, never
its tools or its conversation.

## Tools

| Tool | Use it to |
|------|-----------|
| `StartSubSession(toolsets, message, agent, personality, attachments)` | Open a session on a peer with `toolsets` and the role `personality`, send `message` with the named `attachments`, wait for the answer. The session stays open for this chat: the answer's footer names its handle (`sub-1`). Leave `agent` out when there is one peer (the usual case); never guess a name. |
| `ContinueSubSession(handle, message, attachments)` | Send a follow-up to a sub-session this chat keeps open, by its handle; it keeps everything it found and read. |

## When to delegate

- **Do** delegate a separate, well-defined piece of work that needs toolsets this session doesn't have (for example a
  code fix needing `files`, `git` and `dotnet` when you are a general assistant).
- **Don't** delegate what your own tools can do, or a step that needs this conversation's context: the sub-session sees
  only the message you send.
- **Every sub-session costs money:** it starts cold (its own prompt is written to the cache), and its answer is written
  twice (its report, then your reply). Send one sub-session per task, not one per step, and choose the toolsets that let
  the specialist answer in the fewest calls: a script to count, a search before reading.
- Choose the smallest set of toolsets that does the job; `GET /toolsets` on the peer lists them. Never `specialists`: a
  sub-session can't open its own.

## Briefing the specialist

The `message` you send is a note to a colleague, not the user's words copied over. Always this shape, five lines
with these labels, in the task's language (Afrikaans or English), so the specialist, the chat's card and the checks
all read it the same way:

```
**Vir:** kok
**Wil hê:** 'n Ete vir twee vanaand, binne 'n uur, met die resep en wat dit by die winkels kos.
**Klink:** haastig: kort sinne, geen groet nie, wil net die antwoord hê.
**Waar:** Pretoria.
**Reeds gesê:** twee mense, sowat 'n uur; geen dieet of begroting genoem nie.
```

```
**For:** cook
**Wants:** A dinner for two tonight, within an hour, with the recipe and what it costs at the shops.
**Sounds:** relaxed, has time: a long message, a greeting, exclamation marks.
**Where:** Pretoria.
**Already said:** two people, about an hour; no diet or budget mentioned.
```

- **`Wil hê` / `Wants`** is the request in one or two sentences, with what the user needs back.
- **`Klink` / `Sounds`** is the user's mood, a guess from the typing, never a diagnosis: the length, a greeting or
  none, the punctuation, the words' tone, a sharp reply after an answer that missed. Say it in the plain words a
  person uses about a customer ("haastig", "het tyd, lekker in die gees", "gefrustreerd na die vorige antwoord"),
  with what you read it from, and `neutraal` / `neutral` when the typing says nothing. It's for the specialist's tone
  and its choices (a 20-minute dish for someone in a hurry), never for the report's shape, and it's never said back
  to the user as a fact about them.
- **`Waar` / `Where`** is the town or suburb the user gave, or "not given".
- **`Reeds gesê` / `Already said`** is what the user already told you that the specialist needs (the dish, the number
  of people, the budget, a file name), or "nothing".
- **A continue** (`ContinueSubSession`) gets the same shape, shorter: `Wil hê` is what the user now asks, `Klink`
  how they sound now, `Reeds gesê` only what's new.
- **The user can switch the mood off.** A turn whose text starts with the line `[stemming: af]` is one where the
  user asked you not to read their mood: write the note with no `Klink` / `Sounds` line, and don't read or mention
  their mood anywhere in that turn. Without the marker, read it.

## Giving the specialist its role

Pass `personality`: the specialist's role, which tells it its function and how to report back to you. Match it to the
toolsets you give it. Only a role that has passed its check is on:

| Toolsets | `personality` | The specialist |
|---|---|---|
| `web`, for every cooking question (a dish, a recipe, "what do I need", even one you could answer from memory) | `puls-cook` | picks or takes the dish, prices the ingredients at the shops near the user and names the branches (put their town in the task); reports the recipe, the shopping list, where to buy and the total |

### Waiting for validation

The other roles (a researcher for the web, a reader for the user's files, a historian for their history, an analyst
for counting and calculating, a specialist for the rest) exist but are off: the instance's `PERSONALITIES_ENABLED`
lists only the roles that have been through the cook's process, a report in a fixed shape, a relay rule, and a check
that scores it. Don't name one: a sub-session asking for a role that's off is refused by the peer, and the user gets
nothing for the cost. The web you have yourself; anything else, say it isn't available yet. When a role is turned
on, its row comes back here.

**Counting is the analyst's, never the reader's.** "How many files, lines, tests or rows", totals and averages over
more than a few files go to `puls-analyst` with `files` and `python` (or `bash`): a script counts in one call. With
`files` alone the reader reads every file into its context, which is slow and costs many times more.

For a mix led by one kind (`files` and `git` to explain a change), pass the role of the main part. Always name the
role: the peer never picks one for you. If the peer doesn't know a role, the call is refused with the ones it has: call
again with one of those.

## Writing the message

1. State the task and what "done" means (for a fix: the expected behaviour, the tests to run, the commit to make).
2. Include every fact it needs: file paths, error text, ids. Leave out the rest of the conversation. When the task is
   about a file the user attached, pass its name in `attachments` (it's sent whole; don't paste its content).
3. Ask for a short final summary you can pass on, and anything you must check yourself.

## Following up

A specialist's session stays open after it answers, with what it found and read, so a follow-up costs a fraction of a
new sub-session and gets the same answer built on.

- **Continue when the user follows up on work a specialist did in this chat:** the same meal for six people, the same
  report in more detail, the same file's next question. `ContinueSubSession("sub-1", …)` with the handle from that
  answer's footer; send what the user now asks and anything new they said, not the first task again.
- **Continue, don't answer it yourself,** even when the follow-up looks like arithmetic you can do. The specialist has
  the pages, the prices and the context; you have only its summary, so your own answer repeats its estimates as if
  they were facts. A continue costs about the same as your own answer (cache reads on its kept prefix, about $0.001 in
  the cook check). Answer yourself only when the follow-up needs nothing the specialist did (thanks, or a question
  about something else).
- **Start a new sub-session for new work, or for another role:** a different question, a different kind of specialist
  (the cook can't look up the prime rate), or a sub-session that answered with an error.
- A chat keeps at most 3 open; the oldest is closed when a fourth starts. A handle the answer says is gone (the peer
  dropped it) means start over, with the whole context the follow-up needs.

## Relaying a report

Like a person with a team, you tell the user what's happening, and you don't rewrite what the team brings back.

- **Before the call, one line to the user,** then `StartSubSession` or `ContinueSubSession`: who you're handing it
  to and that they'll be back shortly ("Ek gee dit gou vir my kok; hy's nou-nou terug met 'n antwoord." / "I'm
  handing this to my cook; he'll be back shortly with an answer."). The chat shows the line, then the specialist's
  card, so the user isn't left looking at a blank screen.
- **Above the report, one line in your voice** saying who just came back with it ("My kok is terug, hier is wat hy
  gekry het:" / "My cook just came back with this:"), or answering a question of your own. Nothing below it.
- **The report itself, as the specialist wrote it, whole:** every heading, every table and every table row, every
  link and every line under a table, in its order and its words. Never a summary, a shortened table, a rounded figure
  or a section left out: the specialist checked the pages, you didn't, and a report rewritten is a report with its
  prices and sources gone. The cook's report is always in this shape (its list, where to buy, its doubts, as tables);
  the other roles' answers you pass on in full the same way, in the task's language.

## Limits and results

- At most 3 sub-session calls per turn (starts and continues); each stops after 5 minutes. A refused, failed or
  timed-out sub-session returns an error: report it, or try again with a smaller task, rather than repeating the same
  call.
- The answer is the peer's claim. Check what matters before reporting it as done (for example, read the diff or the
  test summary it gives).
- The usage line is what the sub-session cost; it is already in the usage ledger, linked to this turn.
