---
name: agents
description: The agents toolset's guide, loaded with it. How to ask another agent by its address and use its answer.
---
# Agents Skills

How to ask another agent with `AskAgent`. Another agent answers in its own voice, with its own knowledge and tools;
you get back its answer, never its tools or its conversation.

## Tools

- **`AskAgent(address, message)`**: asks the agent at the address and waits for its answer. An address is an agent's
  name (`acme`), or its name and one of its roles after a dot (`acme.cook`), and on another company's server that
  server's name before a colon (`partner:acme`). You may ask only the agents you are told you may call; any other
  address is refused with the ones you may.
- **`AskAgent(address, message, tool, stronger)`**: asks a minion (*Minions* below): its address, the task as the
  message, and one of your own tools for it to use.
- **`ReadAgentWork(address)`**: reads what an agent you asked in this chat did, step by step: what it was asked, also
  by the person in its own window, what it said, each tool it called with what it sent, and what came back, without
  its thinking. Use it when someone asks what the agent did or why, or when the line under its answer isn't enough.
  When the work is long you get its latest part.

## How to ask

- **Write the message to stand on its own.** The agent sees only your message and the files you name, never this
  conversation: say who is asking, what they want, and anything they told you that it needs.
- **Asking the same address again carries on.** In this chat the agent remembers what you asked it before, so a
  follow-up ("book it for Friday then") needs only what is new. If the answer says it came from a new conversation,
  the earlier one had closed and the agent remembers none of it: if the answer missed something for that, ask again
  with everything it needs.
- **Ask one thing at a time, in the words the person would use.** The agent answers people; ask it as one of them.
- **Use its answer as its answer.** It speaks for itself: pass on what it said and say it was the agent who said it,
  don't change its facts or prices.
- **The line under the answer says what it did.** `did: ShowPlace: Shown; SendLead: Sent` lists each tool it called
  for that answer with the first line of what came back, `failed:` after one that failed, `no tools` when it only
  answered. Say what the agent did from that line alone, never from a guess: if it says `no tools`, nothing was sent
  or booked.
- **Another company's server: say so first.** An address with a server before a colon sends the person's words out
  of this hosting, to another company. Before you ask it, tell the person in one line who you are asking and that
  their question goes there.
- **At most three asks a turn.** If an answer doesn't settle it, tell the person what you learned rather than asking
  again and again.

## Minions

A minion is a worker on a cheap model (the instance's `MINION_LLMS` says which) that uses one of **your own** tools on
one task and answers in its role's fixed shape. What it reads never enters your context: you get its lines back.

- **Your role file lists your minions**, by their addresses (your identity and the minion's function, `acme.lookup`),
  what to hand each (the tool and the task's shape) and what its answer looks like. A minion runs here, never on
  another server, and asks no one.
- **Worth it for reading you'd otherwise do yourself.** Search results and pages stay in your context and are paid
  for again on every later request of this turn; a minion reads them on the cheap model and hands you a few lines.
- **One task, one tool, one answer.** You decide what to look for and use the answers; the minion only finds and
  reports. A minion that needs two tools is two minions, or your own work. The tool must be one you hold, never a
  delegation or skills tool.
- **Write the task in English, self-contained:** everything it needs to find the one thing, as your role file's
  example shows. It sees nothing of this conversation and nothing you found elsewhere. It answers once and closes.
- **A minion's answer is a claim on a cheap model.** Use only what its lines say: a value with its `url` and `quote`,
  or `unknown`. Never fill a gap from memory. When the answer fails your check, `stronger: true` runs the same task
  once on the next model in its list, when the instance lists one.
- **At most 16 minions per user turn**, counted across every agent working on that turn; each stops after 2 minutes.
  A refused, failed or timed-out minion returns an error: what it was looking for is unknown; don't look for it
  yourself and don't repeat the call. What it cost is in the usage ledger and the chat; never put it in your answer.

## When it goes wrong

The answer starts with `Error:` when the address is wrong, the agent can't be reached, or it didn't answer in time.
The error says what to do: use an address it lists, or tell the person the agent couldn't answer. When it says the
agent is still answering, it is busy with an earlier message: wait for that answer, then ask again.
