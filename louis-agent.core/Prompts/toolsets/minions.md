# Minions Skills

How to hand one small task to a minion with `StartMinion`. A minion is a worker on a cheap model (the instance's
`MINION_LLMS` says which) that uses one of **your own** tools on one task and answers in its role's fixed shape. What
it reads never enters your context: you get its lines back.

## Tools

| Tool | Use it to |
|------|-----------|
| `StartMinion(role, tool, task, stronger)` | Open a minion with `role`, hand it one of your tools (`tool`) and the `task`; wait for its answer. It answers once and closes. `stronger: true` runs the same task on the next, stronger model in the role's list, once, only when the instance lists one. |

## When a minion is worth it

- **Reading you'd otherwise do yourself.** Search results and pages stay in your context and are paid for again on
  every later request of this turn; a minion reads them on the cheap model and hands you a few lines.
- **One task, one tool, one answer.** You decide what to look for and you use the answers; the minion only finds and
  reports. A minion that needs two tools is two minions, or your own work.
- **Not for what you already know.** The tool you hand must be one you hold.

## The roles

Your own role file lists the minion roles you may hand work to, what to hand each (the tool and the task's shape) and
what its answer looks like; the minion roles come with your identity, named `<product>-minion-<function>`.
`StartMinion` refuses a name that isn't one.

## Writing the task

In English, self-contained: everything the minion needs to find the one thing, as your role file's example shows. The
minion sees nothing of this conversation and nothing you found elsewhere.

## Using the answer

A minion's answer is a claim on a cheap model. Use only what its lines say: a value with its `url` and `quote`, or
`unknown`. Never fill a gap from memory.

## Limits and results

- At most 16 minions per user turn, counted across every specialist working on that turn; each stops after 2 minutes.
  A refused, failed or timed-out minion returns an error: what it was looking for is unknown; don't look for it
  yourself and don't repeat the call.
- What the minion cost is in the usage ledger and the chat, linked to this turn; never put it in your answer.
