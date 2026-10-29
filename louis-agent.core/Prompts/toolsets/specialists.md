# Specialists Skills

How to hand a piece of work to a specialist agent with `StartSubSession`. A sub-session runs on another louis-agent
(a peer in `AGENT_PEERS`) with only the toolsets the task needs; you get back its final answer, never its tools or its
conversation.

## Tools

| Tool | Use it to |
|------|-----------|
| `StartSubSession(toolsets, message, agent, role, attachments)` | Open a session on a peer with `toolsets` and its `role`, send `message` with the named `attachments`, wait for the answer. The session stays open for this chat: the answer's footer names its handle (`sub-1`). Leave `agent` out when there is one peer (the usual case); never guess a name. |
| `ContinueSubSession(handle, message, attachments)` | Send a follow-up to a sub-session this chat keeps open, by its handle; it keeps everything it found and read. |

## When to delegate

- **Do** delegate a separate, well-defined piece of work that needs toolsets this session doesn't have (for example a
  code fix needing `files`, `git` and `dotnet` when you are a general assistant).
- **Don't** delegate what your own tools can do, or a step that needs this conversation's context: the sub-session sees
  only the message you send.
- **Every sub-session costs money:** it starts cold (its own prompt is written to the cache), and its answer is written
  twice (its report, then your reply). Send one sub-session per task, not one per step, and choose the toolsets that let
  the specialist answer in the fewest calls: a script to count, a search before reading.
- Choose the smallest set of toolsets that does the job; `GET /v1/toolsets` on the peer lists them. Never `specialists`: a
  sub-session can't open its own.

## Giving the specialist its role

Pass `role`: the specialist's role, which tells it its function and how to report back to you. Match it to the
toolsets you give it: your own routing lists the roles this product has and which toolsets each takes, and the shape
of the note you brief it with. Only a role the peer's `ROLES_ENABLED` lists is on: a sub-session asking for one that's
off is refused by the peer, with the ones it has, and the user gets nothing for the cost; call again with one of those,
and never guess a name. For a mix led by one kind (`files` and `git` to explain a change), pass the role of the main
part. Always name the role: the peer never picks one for you.

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
  to and that they'll be back shortly, in your own voice (your routing gives the words). The chat shows the line, then
  the specialist's card, so the user isn't left looking at a blank screen.
- **Above the report, one line in your voice** saying who just came back with it, or answering a question of your
  own. Nothing below it.
- **The report itself, as the specialist wrote it, whole:** every heading, every table and every table row, every
  link and every line under a table, in its order and its words. Never a summary, a shortened table, a rounded figure
  or a section left out: the specialist checked the pages, you didn't, and a report rewritten is a report with its
  prices and sources gone. Every role's answer you pass on in full the same way, in the task's language.
- **The footer the tool adds under the report** (`[Sub-session sub-1 on {peer} ({toolsets}; {role}). It stays open: …]`)
  is for you, not part of the report: its handle, never in your answer.

## Limits and results

- At most 3 sub-session calls per turn (starts and continues); each stops after 5 minutes. A refused, failed or
  timed-out sub-session returns an error: report it, or try again with a smaller task, rather than repeating the same
  call.
- The answer is the peer's claim. Check what matters before reporting it as done (for example, read the diff or the
  test summary it gives).
- What the sub-session cost is in the usage ledger and the chat, linked to this turn; never put it in your answer.
