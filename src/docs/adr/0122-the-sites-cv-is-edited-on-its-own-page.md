# 0122. louis-agent.site: the CV is one record, edited on its own page

**Status:** Accepted · **Date:** 2026-11-13 (Louis, F142: "can we have a resume area for the admin section where I
can edit my resume in a userfriendly manner?", then, having seen the forms, "can we not just edit it inline?") ·
**Recorded:** 2026-11-13 · **Scope:** `louis-agent.site` only (Louis: "it is only for .Site") · Builds on
[0111](0111-the-frameworks-website-lives-in-src-site.md)

## Context

louis-agent.dev shows Louis's CV at `/cv`, drawn from one record (`cv.json`, the JSON Resume shape), and Louis wanted
to change it himself, in the site's admin, without a deploy.

The first editor was a form for each part of the CV beside a live preview of the page, his pick of three. Built, it
held every field twice: once in a form and once on the page. He saw it and asked to edit the page itself. Three ways
of typing on a page were put to him: a box dressed as the text, the text itself made editable (a script, and a fight
with Blazor over the same words), and click, edit, confirm.

## Decision

**On louis-agent.dev, the CV's page is its own editor.** In the admin, the view that draws `/cv` is drawn again with
someone listening for a change, and the CV is changed where it stands:

- Words are clicked and a box takes their place, with a tick and a cross. Enter or the tick keeps, Escape or the
  cross drops.
- A list on the page (links, facts, jobs, points, schools) moves, loses and gains items by small buttons beside
  them. What the page draws as a composed line, or doesn't draw, opens on a small form, kept or dropped as one.
- The view's components learn no edit mode. Each gets its part of the record and hands a changed copy back up, and
  a part has a listener exactly when the whole has one. On the public page nobody listens, so it draws plain words.
- Edits go into a draft and reach the site only on Save.

This is the site's decision, and no rule of the framework. The framework's web supplies pieces the editor is built
from (`Components/Editing`), as it supplies buttons and bands, and decides nothing about how another site edits
its pages.

## Consequences

- The page and its editor can't drift apart, since they are one view over one record. A new field is drawn once,
  and where it is drawn is where it is changed.
- Every change is confirmed, which costs a keystroke, and the page doesn't follow each letter as it is typed.
- The public page's components carry listeners they never use there. The page's golden file holds that it draws
  the same without them.
- The few things the page never draws (the headline, the phone, the website, the country) keep a small form beside
  the page, the one place a field is not on it.
- The forms, and step 6's list editor built for them, were removed.

## Links

[The site's design doc](../projects/louis-agent.site.md) ·
[Inline edit (Atlassian)](https://atlassian.design/components/inline-edit) ·
[Presentation Model (Fowler)](https://martinfowler.com/eaaDev/PresentationModel.html)

---
[Docs index](../README.md) · [ADRs](README.md)
