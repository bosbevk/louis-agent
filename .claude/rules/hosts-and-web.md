---
paths:
  - "src/hosts/**"
  - "src/web/**"
  - "src/site/**"
  - "packs/web/**"
  - "demos/*/hosts/**"
  - "demos/*/web/**"
---

# Hosts and web apps: the how of every rule

Loaded when a file of a host or a web app is read or edited: under `src/hosts/`, `src/web/`, `src/site/`,
`packs/web/`, or a demo's `hosts/` or `web/`. A `.razor` file loads `razor.md` too, and a `.cs` file `csharp.md`.

The rules come in this order: a host's own texts, a web app's folders, a page and its components, and running a
host from Rider.

## Each host's own texts

- **A host keeps its own texts in its project's `Messages/` folder**, named for the host, as `AcpLogMessage`,
  `CliUserText`, `OrchestratorToolMessage` and `ProbeUserText` are. Core can't know them.
- **`HostMessageEnumTests` checks them from source.** Every member has a `[Description]` template, and its braces
  are well-formed named holes.
- The message rule itself is in core's guide, under
  [Messages and logging](../../src/engine/louis-agent.core/CLAUDE.md#messages-and-logging). Hosts staying thin is
  under its [Project layout](../../src/engine/louis-agent.core/CLAUDE.md#project-layout).

## A web app's folders

A web app is a site such as `louis-agent.code-review` or `louis-agent.puls`. The framework's web, `louis-agent.web`,
is the library they are built on, and it follows the same rules.

- **Blazor's three folders stay**: `Pages/`, `Layout/` and `Components/`. Inside `Components/` there are
  subfolders by concern. Puls has `Brand/`, `Landing/` and `Chat/`.
- **C# goes in a folder per concern**, one type per file, with the namespace equal to the folder path. The
  framework's web has `Api/`, `Chats/`, `Usage/`, `Storage/` and `Text/` among its folders. Puls has `Text/` and
  `Content/`.
- **No `Services/` and no `Models/`.** Each names a kind of thing, not a concern.
- **`_Imports.razor` carries a `@using` per folder the pages use.**
- **The framework's chat types are in `Chats/`, not `Chat/`.** A namespace named like a type shadows the type for
  every file under its parent.

## A page and its components

- **A page keeps its layout and hands each piece to a component.** State flows down as parameters and up as
  `EventCallback`s. A component never reaches into its page.
- **A `.razor` holds markup only.** Its C# is the `X.razor.cs` code-behind, a `partial class` in the folder's
  namespace. Logic that needs no component (text, search, matching, formatting, sums) is a plain class in a
  concern folder, with NUnit tests. `RazorHasNoCodeTests` holds it.
- **A string parameter is passed as `@Value`, never `Value`.** Razor reads the bare name as a literal.
- **A turn's life is the shared `ChatRunner`**: the draft, the session, the stream and the save. Never a page's
  own copy.

## Running a host from Rider

- **Each host's `Properties/launchSettings.json` holds its run profiles.**
- **A profile holds the settings the host's compose service gets**, as paths relative to the project folder, such
  as `../../logs`: `WORKSPACE_ROOT`, `SKILLS_DIRECTORY`, `LOG_DIRECTORY`, and the price and rates files. The Puls
  API's `puls` profile adds Puls's own.
- **It listens on a port beside the container's**: 5071, 5081, 5091.
- **A new setting that a compose file passes to a container goes in the host's profile too.** So a debug run sees
  what the container sees ([Setup](../../src/docs/SETUP.md#how-settings-are-loaded)).

## Themes

A theme belongs to the code-review app. Adding one is in that app's guide, under
[Add a theme](../../demos/louis-agent/docs/guides/change-the-web-app.md#add-a-theme).
