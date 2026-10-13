# F20 · Puls look: a landing page and a three-panel dashboard, dark, in Puls's colours

> **Status:** built (2026-10-13) · **Milestone:** — (Puls, outside the cost milestones) · **Depends on:** F5 (Puls,
> live delegation)
>
> **In the TODO:** **F20 Puls look** under *Puls* ([TODO](../../TODO.md)) · **Plan:** [plans/F20](../plans/F20-puls-look.md)

Puls (F5) is the general assistant's web app: chat only, every task delegated to a specialist. It works, but it looks
like a single chat page. F20 gives it the shape of a product: a landing page that says what Puls is, and the chat as a
dashboard with a panel that shows the delegated work. The look follows the *Sensai* AI template (dark, soft colour
glows, floating pill navigation, a three-panel app) and the placeholders of Preline's *AI Chat Interface* (search,
chat menus, message actions, sources, model and tool pickers), built in Puls's own code and colours: orange
(`#f58220`), green (`#00723a`, `#38b449` on dark), charcoal and a charcoal-tinted white (`#efefeb`), in Montserrat,
with the ripple logo and the Afrikaans text.

## User stories

**F20-S1 — Know what Puls is.** As a *visitor*, I want a landing page that says what Puls does and how, so that I know
what I'm starting before I chat.
- *Given* `/` *then* a hero ("Elke vraag op pad na 'n antwoord"), how it works in three steps, the features and a way
  into the chat (`/gesels`).

**F20-S2 — See the delegated work beside the chat.** As a *user*, I want the specialist's work in a panel of its own,
so that the answer stays readable while I can still see who did what, with which tools, from which sources, at what
cost.
- *Given* a turn that delegates *then* the right panel shows the specialist, its toolsets, its steps, the sources and
  the cost; *given* none *then* it can fold away.

**F20-S3 — Familiar chat controls.** As a *user*, I want the controls other AI chats have (search my chats, rename,
archive, copy, model and tool pickers), so that Puls feels finished; what isn't built yet says so ("Kom binnekort")
instead of doing nothing.

## What's real and what's a placeholder

Real: the landing page, the dark theme, the three panels and the work panel, copy, rename, archive, search over the
chats (⌘K), topic cards that fill in a prompt, sources taken from the answer's links. Placeholders (they look right and
say "Kom binnekort"): share, edit, regenerate, good/bad response, voice, screenshot, the tools and model pickers, the
profile menu's items, *Kry Plus*, language, *Verken* and *Studio*.

## Not in scope

A light theme switch, accounts and sign-in, and anything that needs the API to change (regenerate, edit, model choice
per chat): each is its own feature when it's wanted.

## What the build changed

- **Step 3, the Werk panel's cost:** it shows Puls's own cost and the specialists' side by side, not one total. The chat's
  usage doesn't include its sub-sessions yet (the F5 roll-up, [plan](../plans/F05-usage-rollup.md)); a sum in the panel
  would count them twice once it does.
- **Built in order 0–5, then F21 and F22, then 6–9** (Louis's call): personalities and the specialists' tools came first,
  so Puls had its voice and its roles before the rest of the UI.
- **Pay as you go and Oor ons** (approved during the build): *Krediete* in the top bar (this chat and this month, from the
  chats in this browser), *Koop krediete* in the profile menu, the usage line with the specialists' cost included; the
  landing page's Pryse and Oor ons (MIT, github.com/bosbevk/louis-agent). Buying credits is F23.
- **A `GET /model` endpoint** so the model picker shows the real model.
- **Gotchas found by looking:** Bootstrap sets an offcanvas's background-color to transparent at its breakpoint (paint the
  sidebar and Werk panel with a gradient); one page on three addresses with the same parameters doesn't redraw (listen
  to `LocationChanged`); a script that resizes the message box must run after the render.
- **Not built:** the brand's legacy folder (the logo set stayed as it was, so nothing needed to move).

---
[Features](README.md) · [Plan](../plans/F20-puls-look.md)
