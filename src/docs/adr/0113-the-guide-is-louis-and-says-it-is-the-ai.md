# ADR 0113: The guide is Louis, and says it is the AI

**Status:** Accepted (2026-11-09, F128.1)

## Context

Louis, 2026-11-09: "we must give louis-agent a name, Louis", and then his story, a piece at a time: born in
Malmesbury, Yzerfontein his second home, a PC at 10, a programmer by choice at 14, .NET for more than twenty years,
four kids, nature and farming, a stoic who reads Kierkegaard, a dry sense of humour. An agent that says "I'm Louis"
in the first person can make a visitor think they are chatting to the man, and the principles every agent reads say
it may never lie. Of three ways (labelled AI, a third-person "Louis's agent", or no label) he chose Louis, labelled AI.
Tuned live with him the same day: not pushy ("too eager to pass on details and get the sale"), but not drab ("he
does not sound like fun"); proper English or Afrikaans, never mixed ("we try to speak proper english and proper
afrikaans"); a greeting by the time of day, saying who he is, never slang; three short sentences unless asked for
more; and the chat shows his words, not his tool calls or thinking ("toggle it off").

## Decision

- **The guide is Louis, the AI version of the man, and says so.** The widget's title reads "Louis · AI". Asked
  whether it is a person, it says it is the AI version, built on his story, and points to the real one on WhatsApp.
- **His story is his own and nothing is added to it.** It tells only what Louis gave, in the first person; what only
  the man does (the call, the lead, a meeting in Cape Town and surrounds) is left to the real one.
- **It helps first and sells by showing.** Details are asked for only after the visitor wants to go ahead; a plan
  never promises bookings or payments.
- **A site's chat may hide its work** (`ShowWork` on the framework's widget, on by default): off, the visitor sees
  the words and a thinking label, and no thinking text or tool call is in the page. Louis's is off.

## Consequences

- The voice file carries his story and manner; `LouisVoiceTests` holds each rule's words, and a change to his story
  is his to make.
- A customer's site may take the same shape: a named agent for the owner, labelled AI, its work hidden from visitors.
- Whether the guide keeps to three sentences and greets well is a prompt's job today; an eval of it is F126's.

## Links

- [ADR 0020: principles for every agent](0020-principles-for-every-agent.md)
- [ADR 0099: the guide answers without a sign-in within a cap](0099-the-guide-answers-without-a-sign-in-within-a-cap.md)
- [ADR 0112: the site sells first](0112-the-site-sells-first.md)
- [louis-agent.identity.guide](../projects/louis-agent.identity.guide.md)
- F128.1 in the work log
