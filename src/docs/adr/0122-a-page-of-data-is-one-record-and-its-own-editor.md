# 0122. A page of a person's data is one record, and the page is its own editor

**Status:** Accepted · **Date:** 2026-11-13 (Louis, F142: "redo my CV ... going to be a webpage on louis-agent", "can
we have a resume area for the admin section where I can edit my resume in a userfriendly manner?", "can we not just
edit it inline?") · **Recorded:** 2026-11-13 · Builds on [0041](0041-json-through-records-by-hand-only-for-bytes.md),
[0120](0120-nothing-is-hand-wired-from-the-framework.md) and [0121](0121-every-part-of-the-web-is-ready-for-an-agent.md)

## Context

Louis's CV was a Word file. F142 made it a page of louis-agent.dev at `/cv`, and F143 will have the guide answer
questions about it. A page whose words live in its markup can be read by a person and nothing else: an agent would
have to scrape it, a PDF would be a second copy, and an edit would be a code change and a deploy.

The first editor was a form for each part of the CV beside a preview of the page. Louis saw it and asked to edit the
page itself. Two copies of every field (the form's and the page's) is the same problem again, one level up.

## Decision

1. **The data is one record in a public shape.** The CV is a file in the [JSON Resume](https://jsonresume.org/schema)
   shape, plus two parts of our own (`facts` and `now`), read strictly into records. The page, the editor and, in
   F143, the guide all read that one record. Nothing is typed into markup.
2. **It is kept where it can change without a deploy.** The site's host serves the CV (`GET v1/site/cv`, open) and
   keeps an edited one (`PUT v1/admin/cv`, behind the admin key) as a file beside its logs. The copy built into the
   site is the starting copy and the fallback. A save is read strictly and checked for what a public page mustn't
   hold (an ID number, a salary, a birth date, a street address) before it is kept.
3. **The page is its own editor.** In the admin, the same view that draws `/cv` is drawn with someone listening for
   a change. Words are changed where they stand (inline edit: click, a box with a tick and a cross, Enter keeps,
   Escape drops), a list on the page moves, loses and gains items, and an item with more to it than its words opens
   on a small form. Edits go into a draft (Presentation Model) and reach the site only on Save.
4. **A part can be changed exactly when someone listens for the whole.** No "edit mode" is passed down. Each
   component gets its part and hands a changed copy back up, and `PartChanges` gives a part a listener only when
   the whole has one. On the public page nobody listens, so it draws plain words, proved by its golden file.
5. **What is one site's stays in that site.** The CV's records, view, editor and admin area are in
   `louis-agent.site`. The pieces any edited page needs are in the framework's web (`Components/Editing`).
6. **A site's own admin page is no public page.** An address under `/admin` is never prerendered for a search
   engine and never in the list the guide searches.
7. **One page, two outputs.** "Save as PDF" is the browser's print dialog over a print stylesheet. No PDF library,
   and no second copy of the CV.

## Consequences

- An edit shows on the site at once, and the page, the PDF and the guide can't tell different stories.
- A new field is a record member, a place in the view and, there, its way of being changed. There is no form to
  keep in step.
- What the page doesn't draw (the headline, the phone, the website, the country) has no place to be clicked, so it
  keeps a small form beside the page.
- The saved file lives on the host's share. Losing the share loses the edits, not the CV: the built-in copy shows.
- The inline pieces confirm every change, which costs a keystroke and means the page doesn't follow each letter.
  Louis picked that over a box dressed as the text and over the text itself made editable.

## Links

[The site's design doc](../projects/louis-agent.site.md) · [The web's design doc](../projects/louis-agent.web.md) ·
[JSON Resume](https://jsonresume.org/schema) ·
[Presentation Model (Fowler)](https://martinfowler.com/eaaDev/PresentationModel.html) ·
[Inline edit (Atlassian)](https://atlassian.design/components/inline-edit)

---
[Docs index](../README.md) · [ADRs](README.md)
