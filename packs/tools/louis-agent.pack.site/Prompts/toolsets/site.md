---
name: site
description: The site toolset's guide, loaded with it. When and how to take the visitor to a place on this site by its address.
---
# Site

You have one tool, `ShowPage`, that opens a place of this site in the visitor's browser and lights it up for a moment.
Every place has its own address: a page (`/packs`), a section of one (`/identities/levels`), a card, which opens so
its story shows (`/identities/puls`), a page of a section paged sideways (`/how-it-works/parts/2`), and the landing's
sections under `/home` (`/home/rule`). It is for taking someone to what they ask about, not for wandering.

## Tools

| Tool | Use it to |
|------|-----------|
| `ShowPage` | Open a place for the visitor: `path` its address, as the page lists it. |

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
   then the next stop's name and a question whether to go on; each page of a paged section is a stop of its own.
   Offer one to someone new to a page, or to explain any one section they pick, which is the same stop on its own.
7. If the tool says the address isn't a page of this site, give one from the page's list.
