# louis-agent.puls — design

> **Kind:** Blazor WebAssembly app · **Path:** `src/louis-agent.puls/` · **Served by:** louis-agent.api built with
> `-p:WebApp=puls` (`docker/Dockerfile.puls`, `docker/docker-compose.puls.yml`, port 5070) · **References:**
> louis-agent.web.shared, Bootstrap 5.3 (CDN, SRI-pinned), highlight.js (CDN), Montserrat (Google Fonts)

## Purpose

Puls is the general assistant's web app: a South African AI assistant in everyday Afrikaans that answers by handing each
task to a specialist (F5) and shows that work as it happens. It's chat only: its sessions load the `agents` toolset and
nothing else, so every task is delegated to its peer (`puls-agent`), with a role (F21) and only the toolsets the task
needs. Pay as you go: every answer shows what it cost, specialists included.

## How it works

- **Pages:** `/` is the landing page (`LandingPage.razor`). The app is one page, `ChatPage.razor`, at `/gesels`,
  `/gesels/{id}` (old `/chat/{id}` links move there), `/verken` and `/studio`; it redraws on every address change, since
  Blazor wouldn't for the same page with the same parameters.
- **A chat** opens `POST /sessions` with `toolsets: ["agents"]`, `personality: "puls"` (F21) and, when the user shared it,
  `location` and `time_zone` (F22). Messages stream over SSE; `TurnStream` (shared) folds them, sub-session events
  included, into the turn the page shows.
- **Three panels:** the sidebar (search, Verken, Studio, favourites, projects, recent, archive, profile and settings),
  the chat (top bar, messages with actions, the message box), and the Werk panel (`WorkPanel.razor`: each delegation's
  specialist, toolsets, task, steps and cost, the chat's sources, Puls's and the specialists' cost). Below 1200px the
  Werk panel slides in; below 992px the sidebar does.
- **Placeholders** (share, edit, regenerate, feedback, voice, screenshots, image, deep research, other models, language,
  theme, profile, sign-out, buying credits) answer with a "kom binnekort" toast.
- **Stored in the browser:** chats (with favourite and archived), the API key, the currency, the location, and whether
  the sidebar and the Werk panel are hidden.

## Structure

| Path | What |
|---|---|
| `Pages/LandingPage.razor` | Hero, a picture of the app in HTML, how it works, features, Pryse (pay as you go), Oor ons, FAQ |
| `Pages/ChatPage.razor` | The shell: sidebar, top bar, the chat, Verken and Studio views, the message box, the Werk panel |
| `Components/PulsLogo.razor` | The ripple logo inline (its rings glow outward) |
| `Components/WorkPanel.razor` | The Werk panel |
| `Components/ChatSearch.razor` | Ctrl/Cmd K search over the chats in this browser |
| `Components/ExploreView.razor`, `StudioView.razor`, `TemplateCard.razor` | Verken and Studio |
| `PulsText.cs` | The shared components' words in Afrikaans (`ChatText`) |
| `Sources.cs` | A turn's sources: the answer's links and the pages its specialists read |
| `Templates.cs` | The starters on Verken and Studio |
| `wwwroot/css/app.css` | The dark theme (orange, green, charcoal, a charcoal-tinted white; Montserrat) |
| `wwwroot/js/chat.js` | Scrolling, the sidebar and panel toggles, copy, location (browser + Nominatim), focus |
| `wwwroot/brand/` | The logo set: mark, logo, stacked, icon tiles, colour versions |

## Configuration

None of its own: the API it's served by holds the settings (`AGENT_PEERS` names `puls-agent`). The compose file runs
`puls` (5070, the app and its API) and `puls-agent` (5071, the specialists' API, no peers of its own).

## Extending it

- **A starter:** a line in `Templates.cs`.
- **Puls's voice or a specialist's role:** `Skills/personalities/puls*.md` (F21); a file on disk overrides the built-in copy.
- **A placeholder made real:** replace its `ComingSoon(...)` with the feature; keep the label.

## Tests

None automated (like louis-agent.web): checked by hand and by headless-Chrome screenshots with sample chats at 1440,
1100 and 420 px wide. The API's sessions and core's tools under it are tested.

## Limits and plans

- Usage figures are per browser (the chats saved here); a user's monthly total belongs to the server-side ledger (F23).
- Buying credits (F23 Credits), a light theme, accounts, and the placeholders above are to come.

## Related docs

[F20 Puls look](../features/F20-puls-look.md) · [F21 Personalities](../features/F21-personalities.md) ·
[F22 Role tools](../features/F22-role-tools.md) · [louis-agent.web](louis-agent.web.md) · [api](louis-agent.api.md)

---
[Projects](README.md)
