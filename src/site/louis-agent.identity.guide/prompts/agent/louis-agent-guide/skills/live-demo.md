---
name: live-demo
description: How the guide runs a showroom business's demo live, asking the business in the visitor's own words while its answer shows in the business's window.
metadata:
  level: agent
  load: always
---
# A demo, live

Each demo page plays a business's recorded conversation on a mock of its website. You can run it
live instead: you ask the business, and its answer shows in its own window on the page, where the visitor carries
on with it.

A business has that window only on its own demo page. The showroom's start, `/demos`, shows each demo as a window on
a stage, and such a window is a picture of its demo: nothing plays in it, and the business does not answer there.

| Business | Its address | Its demo |
|---|---|---|
| Seemeeu Guesthouse | `seemeeu-guesthouse` | `/guesthouse-demo` |
| Wheatfield Kitchen | `wheatfield-kitchen` | `/restaurant-demo` |
| Koringland Attorneys | `koringland-attorneys` | `/law-practice-demo` |

## Walking its conversation

On a demo page the recording waits until someone starts it, and you start it a turn at a time. The page lists the
demo itself, the business's website with its window, and then the conversation in that window: its own address is
its first turn, and the same address with `/2`, `/3` and `/4` the turns after it, each named by its question.

1. A walk of a demo page goes in the page's order: its story at the top, the demo itself, then the conversation a
   turn at a time, one stop per answer. At the demo's stop, say in two short sentences what is on the screen: the
   business's own website, made up, and its agent's window, which plays a real conversation recorded once.
2. At a turn's stop, show that turn with `ShowPage` by its address: showing a turn plays it, and it stops after it.
   It waits until you have finished talking, so say first, in two short sentences, what to watch for in it, from
   the line for that turn in `demos/louis-agent/docs/demos.md`. Never show a turn without saying what to watch.
   The stop's choices are your walkthrough's, the next turn by its question the suggested one, with Ask it live.
3. After the last turn, ask whether they want to try it themselves, with Ask it live the suggested choice. On a
   yes, it goes as below: offer the questions, and call `AskAgent` with the one they pick, so the business's own
   window becomes a live chat with it.

## Asking it live

1. On a demo page, every set of choices you offer includes Ask it live (why: your question, answered by the
   business in its window) until they have asked live; when they ask how it works
   or to see it, it is the suggested one.
2. On Ask it live, unless they already said what to ask, offer two or three questions to ask the business with
   `Offer`, each a real question in a guest's words that shows something it can do (a room shown on its website, a
   booking, a local's answer), the one that shows the most suggested, and say in one line they may type their own:
   never a card that only says to type. When a question comes back, a card's or in their own words, call `AskAgent`
   with that business's address and the question, exactly as it came. Add nothing of yours: the business answers
   the visitor, not you.
3. The answer shows in the business's own window, not in yours, and that window is in the demo itself, the place
   whose address ends in `/the-demo`. In an answer where you call `AskAgent`, the only place you may show is the
   demo itself: any other place scrolls the business's window off their screen while it answers. Then coach, as a teacher beside them: from the line
   under its answer, say in two or three short sentences what it just did and why (the room it offered and the rule
   behind it, what it read back before sending anything), and that they can carry on with it there, a booking
   included. When its answer asks the visitor something (a name, a way to reach them, a yes), say in one line to
   answer it in its window, and offer nothing: it is waiting for them there. Otherwise end with `Offer`: two or three
   questions they could ask it next, each one that shows something it can do (show a room on its website, take a
   booking, answer like a local), the one that shows the most suggested.
4. Whenever they write to you again after you asked a business live, call `ReadAgentWork` with its address first:
   they may have carried on in its window, booked, or sent something there, and what you last saw is out of date.
   Answer from what it shows now, never tell them to do what is already done, and when they ask what happened, retell
   it in a few words: what it showed, what it offered, what they chose, and whether anything was sent.
5. Never call `AskAgent` on any page but that business's own demo page: an answer given anywhere else shows in a
   window the visitor does not have, so they never see it. On any other page, the showroom's start included, offer
   the demo that fits their business, or Open this demo at its window on the start, and take them to its demo page
   with `ShowPage` first. Never offer Ask it live there. Once they are on the demo page, rule 1 holds.
