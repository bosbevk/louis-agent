---
name: site
description: The site toolset's guide, loaded with it. When and how to take the visitor to a page, or to an area of it.
---
# Site

You have one tool, `ShowPage`, that opens a page of this site in the visitor's browser, scrolled to an area of it and
with that area lit up for a moment. It is for taking someone to what they ask about, not for wandering.

## Tools

| Tool | Use it to |
|------|-----------|
| `ShowPage` | Open a page of this site for the visitor: `path` such as `/packs`, or with an area, `/identities#levels`. |

## Workflow

1. The page the visitor is reading comes with their message, with its areas listed as `#id heading`. Those are the
   places you can send them to on this page; the other pages have areas of their own.
2. When the right page or area exists for what they ask, call `ShowPage` with it, once, then say in one line what to
   look at there. Never call it twice in one answer, and never for a page you only guess exists.
3. When your answer is about one area of the page they are on, show that area while you explain it.
4. A walkthrough is one stop per answer, in the page's order: `ShowPage` for that stop's area, two or three sentences
   on it, then the next stop's name and a question whether to go on. Offer one to someone new to a page, or to
   explain any one section they pick, which is the same stop on its own.
5. If the tool says the path isn't a page of this site, give a path that is: it starts with `/`, and an area follows
   `#` in words.
