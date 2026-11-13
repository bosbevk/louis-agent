---
paths:
  - "**/prompts/**/*.md"
  - "**/*ToolMessage.cs"
---

# Prompts and tool texts: the how of every rule

Loaded when a prompt file or a tool's message enum is read or edited. These are the texts our own agents read: an
identity's voice, a specialist's role, a minion, a toolset's guide, the principles, and what a tool answers. The
why: [ADR 0011](../../src/docs/adr/0011-prompts-are-content.md) (prompts are content),
[ADR 0018](../../src/docs/adr/0018-built-in-prompts-embedded.md) (built-in prompts are embedded) and
[ADR 0063](../../src/docs/adr/0063-a-prompt-says-what-it-is.md) (a prompt says what it is).

- **Only what the model needs to read.** Louis, 2026-11-04, of a name and a date in a line of Puls's voice: "this
  should never happen". A prompt goes to the model on every request, so a developer's note in it costs tokens
  every time and tells the model nothing. Never "(Louis, 2026-11-04)" or the like in a prompt, in a tool's
  description or in a tool's message. Who decided, and when, goes in the plan, an ADR or the commit message.
- **Every prompt opens with its header** (ADR 0063). `name` is the file's own name without `.md`. `description`
  says what it is and when it is loaded, in at most 1024 characters. A mode, a role or a minion names its
  `metadata.level`, and a role may name its `metadata.toolsets`. The reader is strict: an unknown key, a name
  that isn't the file's, or a missing description refuses the file and says what to change. `PromptHeadersTests`
  holds it.
- **A changed prompt or tool text changes a golden file.** What the model reads is pinned. So regenerate the
  golden files and review their diff before the step is shown. The commands are in `src/docs/guides/write-tests.md`,
  under *Regenerate a golden file*.
- **A built-in prompt is read only from the assembly** (ADR 0018). A host or a container that is running shows
  a changed prompt only after a build.
