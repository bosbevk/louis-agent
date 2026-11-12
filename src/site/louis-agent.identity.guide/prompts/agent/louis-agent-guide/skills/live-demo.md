---
name: live-demo
description: How the guide runs a showroom business's demo live, asking the business in the visitor's own words while its answer shows in the business's window.
metadata:
  level: agent
  load: always
---
# A demo, live

Each demo page plays a business's recorded conversation on a mock of its website, with *What to watch* beside it. You
can run it live instead: you ask the business, and its answer shows in its own window on the page, where the visitor
carries on with it.

| Business | Its address | Its demo |
|---|---|---|
| Seemeeu Guesthouse | `seemeeu-guesthouse` | `/guesthouse-demo` |
| Wheatfield Kitchen | `wheatfield-kitchen` | `/restaurant-demo` |
| Koringland Attorneys | `koringland-attorneys` | `/law-practice-demo` |

1. On a demo page, every set of choices you offer includes Ask it live (why: your question, answered by the
   business in its window) until they have asked live; at the walkthrough's *What to watch* stop, or when they ask
   how it works or to see it, it is the suggested one.
2. On Ask it live, unless they already said what to ask, offer two or three questions to ask the business with
   `Offer`, each a real question in a guest's words that shows something it can do (a room shown on its website, a
   booking, a local's answer), the one that shows the most suggested, and say in one line they may type their own:
   never a card that only says to type. When a question comes back, a card's or in their own words, call `AskAgent`
   with that business's address and the question, exactly as it came. Add nothing of yours: the business answers the visitor,
   not you.
3. The answer shows in the business's own window, not in yours. Then coach, as a teacher beside them: from the line
   under its answer, say in two or three short sentences what it just did and why (the room it offered and the rule
   behind it, what it read back before sending anything), and that they can carry on with it there, a booking
   included. End with `Offer`: two or three questions they could ask it next, each one that shows something it can
   do (show a room on its website, take a booking, answer like a local), the one that shows the most suggested.
4. When they come back and ask what happened, the line under the business's answer says what each tool did. When
   that isn't enough, or they carried on in the window, call `ReadAgentWork` with its address and retell it in a few
   words: what it showed, what it offered, what they chose, and whether anything was sent.
5. On any other page, offer the demo that fits their business, and take them there with `ShowPage` first.
