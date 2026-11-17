# The demos

> **Explanation.** What each demo on the site shows, how it was made, what is real and what is played back, and what
> it would mean for a business. The guide in the site's corner reads this to explain the demos. **Elsewhere:** why
> the public watches recordings in [ADR 0108](../../../src/docs/adr/0108-the-public-watches-the-coding-agent-recorded.md),
> how to record them again in `src/site/recordings/record-showroom.sh`.

The site's demos are at `/demos`. Each one is a real conversation with an agent built on the framework, recorded once
and played back in the browser. Nothing runs and nothing is paid for while a visitor watches.

The demos live in a showroom of their own: a rail of every demo down the left, and each demo on a stage in its own
colour. `/demos` is its start, and features the guesthouse.

## Websites with an agent (`/guesthouse-demo`, `/restaurant-demo`, `/law-practice-demo`)

**What it shows.** Three made-up businesses, each with a mock of its own website in its own sample brand, real photos
and its agent in a widget in the corner: Seemeeu Guesthouse in Yzerfontein, Wheatfield Kitchen in Malmesbury (with a
full menu), and Koringland Attorneys. A customer asks the agent about a stay, a table or a will. The agent answers
only from the business's own facts, and every price on the website is one the agent knows. While it answers it shows
the website: it opens the place that answers the question (the garden room, the children's menu, the wills page) with
the same tool the site's own guide uses, and the place is lit. It passes a booking on to the owner only after the
customer says yes, and the law practice never gives legal advice.

**How it was made.** Each conversation was recorded once by a script that asks each business's agent the same few
questions, word for word the ones the page shows, on the site's own model, for a few cents a business. The page plays
that stream at a person's pace, with nothing beside it but the business's website, so the agent is what the visitor
watches. Once it has played, the visitor can try the agent live, and the guide in the site's corner can ask it for
them.

**What is real.** The businesses and their websites are made up, and say so. The agent's words, the places it shows
and the booking it passes on are its own, from that one recording. Only the timing is played back.

**For a business.** The same agent sits in the corner of a business's own website, knows its hours, its prices and
its services from files the owner can read and change, answers its customers day and night, and passes a booking on
after the customer's yes.

## What the demos have in common

They are recordings of real runs, not mock-ups. The words on screen are what the agents said. They cost a few cents
to make, once, and nothing to watch.

---
[Docs index](README.md) · Related: [the site's project doc](../../../src/docs/projects/louis-agent.site.md),
[the showroom's identities](../../../src/docs/projects/louis-agent.identity.showroom.md)
