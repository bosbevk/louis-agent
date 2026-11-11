---
name: agents
description: The agents toolset's guide, loaded with it. How to ask another agent by its address and use its answer.
---
# Agents Skills

How to ask another agent with `AskAgent`. Another agent answers in its own voice, with its own knowledge and tools;
you get back its answer, never its tools or its conversation.

## Tools

- **`AskAgent(address, message)`**: asks the agent at the address and waits for its answer. An address is an agent's
  name (`acme`), or its name and one of its roles after a dot (`acme.cook`). You may ask only the agents you are
  told you may call; any other address is refused with the ones you may.
- **`ReadAgentWork(address)`**: reads what an agent you asked in this chat did, step by step: what it said, each
  tool it called with what it sent, and what came back, without its thinking. Use it when someone asks what the
  agent did or why, or when the line under its answer isn't enough. When the work is long you get its latest part.

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
- **The line under the answer says what it did.** `did: SendLead, ShowPlace` lists the tools it called for that
  answer, `(failed)` after one that failed, `no tools` when it only answered. Say what the agent did from that line
  alone, never from a guess: if it says `no tools`, nothing was sent or booked.
- **At most three asks a turn.** If an answer doesn't settle it, tell the person what you learned rather than asking
  again and again.

## When it goes wrong

The answer starts with `Error:` when the address is wrong, the agent can't be reached, or it didn't answer in time.
The error says what to do: use an address it lists, or tell the person the agent couldn't answer. When it says the
agent is still answering, it is busy with an earlier message: wait for that answer, then ask again.
