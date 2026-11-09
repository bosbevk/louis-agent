# ADR 0108: The public watches the coding agent recorded, and the live agent stays behind the key

**Status:** Accepted (2026-11-09, F120)

## Context

The coding agent loads every toolset its host offers, a shell among them, on the server's workspace. Since FX88 its
chat has been closed to visitors: the site had no sign-in, only the key holder could use it, and the header's "Try
it" and the hero's "Try the coding agent" were a faded "Coming soon" that led nowhere. Selling is the project's goal
(Louis, 2026-11-08: "selling the agent is a top priority"), and a door that leads nowhere sells nothing.

Asked what "done" means for that door, Louis was given three options on 2026-11-09:

- **Watch it work:** a real session, recorded once, played back in the real chat, then the offer. Nothing runs per
  visitor and no stranger touches a shell.
- **A live, read-only sandbox:** a second open door for the coding identity, read-only file and git tools on a fresh
  copy of a sample per session, capped a day. Every visit spends on the key, and a per-session workspace is new
  framework work.
- **Signed-in, full agent:** accounts on the site, and a throwaway container per person with every tool. The most
  real, and the most cost, risk and work.

He picked *Watch it work*.

## Decision

- **A visitor watches the coding agent; they don't run it.** The site's doors lead to `/coding-agent`, where a
  session recorded once plays back. The live chat stays closed to strangers (`SiteDoors.CodingAgent`) and open to
  the key holder.
- **A recording is the stream the API sent**, saved as a `.sse` file beside the site's other files, and played with
  the same reader and the same `TurnStream` as a live chat, so a visitor sees exactly what the agent's own chat
  drew. The pace comes from each event's kind (`RecordingPace`), since a stream carries no times.
- **Playing a recording is the framework's** (`RecordedChat`, `TurnReplay`), so any site can show a recorded
  session of its own agent; the eval studio's replay shares the same loop.
- **A recording runs on a sample, never on this repo.** `demos/louis-agent/recordings/record.sh` copies a sample
  into a fresh git repository and runs the API's own image on it, open on localhost only, with the model key and
  nothing else from `config/`: the host reads every `.env` file in its config folder, so the folder is never mounted.
  Recording is a paid run, asked for first.

## Consequences

- The coding agent sells at no cost per visitor, and the page names what the session cost when it was recorded (about
  R0.10 on Haiku), at the rate of that day.
- A recording ages: when the agent or its tools change, the page still shows the old session until it is recorded
  again. A test holds the page's question to the script's, and that the recording is a whole session.
- A visitor can't ask the coding agent their own question; the guide in the corner and the contact page are where
  that goes. Opening the live agent to strangers is a later decision, with a sandbox, its own ADR.

## Links

- [ADR 0106: the landing is a business website from the framework](0106-the-landing-is-a-business-website-from-the-framework.md)
- [ADR 0007: measure every request, and show it](0007-measure-every-request.md)
- [ADR 0084: every site is built on the framework's web](0084-every-site-is-built-on-the-frameworks-web.md)
- F120 and FX88 in the work log
