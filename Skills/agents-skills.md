# Agents Skills

How to hand a piece of work to a specialist agent with `StartSubSession`. A sub-session runs on another louis-agent
(a peer in `AGENT_PEERS`) with only the toolsets the task needs; you get back its final answer and a usage line, never
its tools or its conversation.

## Tools

| Tool | Use it to |
|------|-----------|
| `StartSubSession(agent, toolsets, message, personality, attachments)` | Open a session on a peer with `toolsets` and the role `personality`, send `message` with the named `attachments`, wait for the answer, close the session. Leave `agent` empty when there is one peer (the usual case); never guess a name. |

## When to delegate

- **Do** delegate a separate, well-defined piece of work that needs toolsets this session doesn't have (for example a
  code fix needing `files`, `git` and `dotnet` when you are a general assistant).
- **Don't** delegate what your own tools can do, or a step that needs this conversation's context: the sub-session sees
  only the message you send.
- Choose the smallest set of toolsets that does the job; `GET /toolsets` on the peer lists them. Never `agents`: a
  sub-session can't open its own.

## Giving the specialist its role

Pass `personality`: the specialist's role, which tells it its function and how to report back to you. Match it to the
toolsets you give it:

| Toolsets | `personality` | The specialist |
|---|---|---|
| `web` | `puls-researcher` | finds things out on the web; reports with sources |
| `files` | `puls-reader` | reads the user's files; reports with paths and quotes |
| `git` | `puls-historian` | reads the repository's history; reports commits |
| `python` (with `files` when the data is the user's) | `puls-analyst` | counts and calculates with a script; reports the method |
| anything else, or a mix | `puls-specialist` | does one piece of work and reports the evidence |

For a mix led by one kind (`files` and `git` to explain a change), pass the role of the main part. If you pass none,
the peer picks one from the toolsets (its `roles.json`). If the peer doesn't know a role, the call is refused with the
ones it has: call again with one of those, or with none.

## Writing the message

1. State the task and what "done" means (for a fix: the expected behaviour, the tests to run, the commit to make).
2. Include every fact it needs: file paths, error text, ids. Leave out the rest of the conversation. When the task is
   about a file the user attached, pass its name in `attachments` (it's sent whole; don't paste its content).
3. Ask for a short final summary you can pass on, and anything you must check yourself.

## Limits and results

- At most 3 sub-sessions per turn; each stops after 5 minutes. A refused, failed or timed-out sub-session returns an
  error: report it, or try again with a smaller task, rather than repeating the same call.
- The answer is the peer's claim. Check what matters before reporting it as done (for example, read the diff or the
  test summary it gives).
- The usage line is what the sub-session cost; it is already in the usage ledger, linked to this turn.
