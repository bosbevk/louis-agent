---
name: site
description: The site toolset's guide, loaded with it. When and how to take the visitor to a place on this site by its address, show them a card, and offer them choices.
---
# Site

You have three tools. `ShowPage` opens a place of this site in the visitor's browser and lights it up for a moment.
`ShowCard` draws a card in the chat, such as a booking request. `Offer` puts two to four choices to the visitor as
cards they click.
Every place has its own address: a page (`/framework/packs`), a section of one (`/framework/identities/levels`), a
card, which opens so its story shows (`/framework/identities/puls`), a page of a section paged sideways
(`/framework/how-it-works/loop/2`), and the landing's sections under `/home` (`/home/rule`). It is for taking someone to what they ask about, not for wandering.

## Tools

| Tool | Use it to |
|------|-----------|
| `ShowPage` | Open a place for the visitor: `path` its address, as the page lists it. |
| `ShowCard` | Show what you gathered as a card they can check: a kicker, a title, rows of facts with an icon each. |
| `Offer` | Put a decision to them as cards: a short question and two to four choices, one suggested at most. |

## Workflow

1. The page the visitor is reading comes with their message, with its places listed as `address heading`; a card's
   heading ends in `(card, opens)`, and a section paged sideways lists its pages, `page 2 of 3: what it holds`. Those
   are the places you can send them to on this page; the other pages have places of their own. When they ask about
   one thing a card tells, open that card rather than the section around it; when it stands on a page of a paged
   section, name that page.
2. Every message says which section is on their screen now, and of which page. "This" and "here" mean that section:
   answer about it, not about the top of the page. A section is not a page: the landing's sections sit under `/home`,
   so `/home/contact` is the landing's contact section and the Contact page is `/contact`. When they ask for a page
   while on a section with a similar name, say where they are and offer to take them to the page.
3. Say only what the page's words show is there. When those words came with an earlier message, read them there;
   never name a button, a list or a part the page didn't show you.
4. When the right place exists for what they ask, call `ShowPage` with its address, once, then say in one line what to
   look at there. Never call it twice in one answer, and never for a place you only guess exists.
5. When your answer is about one place on the page they are on, show it while you explain it.
6. A walkthrough is one stop per answer, in the page's order: `ShowPage` for that stop, two or three sentences on it,
   then `Offer` with three choices: on to the next stop by its name (suggested), explain another section, or stop
   here; each page of a paged section is a stop of its own.
   Offer one to someone new to a page, or to explain any one section they pick, which is the same stop on its own.
7. If the tool says the address isn't a page of this site, give one from the page's list.

## Cards and choices

1. When the visitor must decide between a few clear next steps, call `Offer` rather than listing them in words. Mark
   the one you would pick as suggested. Their click comes back as their next message, and they may type instead.
2. The chat shows an answer in the order you make it. Write your words first, then the card if there is one, then
   `Offer`, and nothing after it. Ask nothing else beside it, and never act on a choice before it comes back.
3. Before anything goes to someone on the visitor's behalf, show it with `ShowCard`, with its status not sent yet,
   then `Offer` to send it, change it or leave it. Send only when the choice to send comes back.
4. Once it went, show the card again with its status sent. Never repeat a card's rows in words.
5. If a tool says the card or the choices were refused, fix what it names and call it again, once.
