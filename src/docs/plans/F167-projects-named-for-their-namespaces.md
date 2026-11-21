# F167 Every project named for its namespace

A plan: every `louis-agent.…` project in the repository takes the name its code already uses, so that
`louis-agent.core` becomes `LouisAgent.Core` on disk, in its project file, in its DLL and, once F166.2 publishes it,
in its package. The work is done by a small rename tool of our own, kept in `src/tools` and tested like any other
code, then run once over the whole repository. This file says what the tool does and why it is shaped this way,
then gives its eight steps.

## Context

F166 moves Puls into its own repository, on the framework as published packages, and F166.2 is the child that
publishes them. While its findings were put on 2026-11-21, Louis chose that a package, its DLL and its namespace
read the same, `LouisAgent.Core`, and then that the projects themselves are renamed, files and folders, as a
feature of its own before F166.2. That is this plan. His picks of that day, each of three options after research:

| # | The choice | His pick |
|---|---|---|
| F166.2, 4 | What the packages are called | C: the assemblies renamed too, so package, DLL and namespace read the same |
| F166.2, 5 | How far the rename goes | C: projects, files and folders, a feature of its own before F166.2 |
| 1 | How the rename is done, since Rider's MCP renames symbols only | B: a rename tool of our own in `src/tools`. Louis: "it is a versioned tool by us, not just a random python script the AI decided on. That is what that ADR is about", and "we keep the tool that's all" |
| 2 | Which projects | B: all 155, Puls's 26 included |
| 3 | What each new name is | A: the project's `RootNamespace`, so `louis-agent.pack.web` becomes `LouisAgent.Packs.Web`; eight that can't be read that way are listed |
| 4 | What the tool rewrites beyond the build files | A: only the exact names of real projects, everywhere; every other `louis-agent.…` word goes to a report |
| 5 | How the run is cut into commits | B: one run, one commit, vouched for by the build, the tests and the report |
| 6 | The `RootNamespace` of the eight listed projects | A: left as it is, so no file's namespace changes |
| 7 | The docs and pictures named for a project | A: renamed with their project, their links with them |

**What was looked up.** The repository tracks 6,951 files. Of them, 1,435 name a project, about 6,000 times,
in C#, JSON, SVG, docs, project files and a few Dockerfiles, Razor, SCSS and YAML files. There are 155
`louis-agent.…` projects, each in a folder of the same name, and every one sets its `RootNamespace` in its project
file. The four sample projects the coding agent works on (`OrderService`, `InvoiceMatcher`) are named .NET style
already and are not touched. 91 `InternalsVisibleTo` lines name an assembly. 463 more files are named for a
project: the 45 project design docs (`src/docs/projects/louis-agent.core.md`) and 418 diagram files, a JSON and
its SVG each (`louis-agent.core-how-it-works.json`). `ProjectDocsTests` holds a doc's overview to its doc's
name, and `SolutionLayoutTests` lists every project by folder. Besides the project names there are 1,034 uses
of other `louis-agent.…` words: the domain `louis-agent.dev` (583), projects that no longer exist, packs that are
only planned, examples in guides, and patterns like `louis-agent.pack`. None of those may be rewritten blindly.

For 141 projects the new name is the `RootNamespace` itself. In seven pairs two projects share one namespace, and
in each pair one already owns it; the other takes the namespace and its own part, and `coding-catalog`, whose
namespace is `LouisAgent.Coding`, keeps its "Catalog". So eight names are listed by hand: `LouisAgent.Evals.Checks`,
`LouisAgent.Identities.Puls.Evals`, `LouisAgent.Identities.Puls.Evals.Tests`, `LouisAgent.Packs.Recipes.Checks`,
`LouisAgent.Packs.Recipes.Checks.Tests`, `LouisAgent.Packs.WhereToBuy.Checks`,
`LouisAgent.Packs.WhereToBuy.Checks.Tests` and `LouisAgent.Coding.Catalog`.

Rider's MCP server has no tool that renames a project, a file or a folder; its `rename_refactoring` renames a
symbol. Rider's own documentation says a project rename in its UI can rename "the project folder and the project's
root namespace", and does not say what it does with the project file, the solution or other projects'
references. That is why the tool is ours.

**Not known yet, said plainly.** Whether a project embeds a resource whose name depends on its file name rather than
its `RootNamespace`; the run's build shows it. And whether ADR 0052 gains one sentence saying a tool we keep,
versioned and tested, is not the home-made script it forbids: put to Louis, not answered; step 8 asks again.

## Design

### 1. What the tool does

The tool reads the name of every project first. It walks git's list of tracked files, finds each
`louis-agent.….csproj`, and reads the new name from its `RootNamespace`, or from the eight listed names when its
namespace is shared. If two projects would end up with the same name, or a shared namespace has no listed name,
it stops and says which, before anything is touched.

Then it rewrites words. In every tracked text file it replaces a project's old name with its new one, but only
where the whole name stands: the longest names go first, so `louis-agent.core.web` is never half-changed by
`louis-agent.core`, and a name only counts when what follows it ends the word, is a file ending such as `.csproj`,
`.dll` or `.md`, or is a `-` that starts a picture's own part (`louis-agent.core-how-it-works`). `louis-agent.dev`
is not a project's name, so it can't match. Every other `louis-agent.…` word it meets goes into its report, with
its file and line, for a person to read.

Last, it moves. Every tracked folder and file whose name begins with a project's name is renamed the same way,
which takes in the project folders and files, the design docs and their pictures. It moves them on the disk and
lets git see the renames when the commit is staged. A check run, `--check`, does all the reading and rewriting in
memory, writes nothing, and prints what it would do and the report.

![The run holds the name map, the rewriter, the leftover words and the mover; it uses git's list of tracked files and makes the report](diagrams/F167-projects-named-for-their-namespaces-interface-design.svg)

### 2. Where it sits, and what proves it

The tool is a console program beside `louis-agent.doc-diagrams`, with its tests beside that tool's tests. They
build small repositories of their own in a temporary folder, with `git init`, so they run the same on Windows and on
Linux. The real run is proved by what already exists: the solution builds, the whole unit suite passes, the
diagrams tool redraws every picture and finds none stale, and the layout tests list every project under its new
name.

![Louis runs the tool's check and then the run; the tool reads git's list, rewrites and moves the repo's files and writes its report; the build and the edits prove and finish it](diagrams/F167-projects-named-for-their-namespaces-architecture.svg)

### 3. The main case: one run

![The names are read first, each tracked text file is rewritten, and only then are folders and files moved; a check run writes nothing](diagrams/F167-projects-named-for-their-namespaces-communication-one-run.svg)

Words are rewritten before anything moves, so every path the tool reads is still where git says it is. The run
holds nothing but the names and the report:

![One run holds the name map of 155 renames, read from the projects, the rewriter over it and the report](diagrams/F167-projects-named-for-their-namespaces-objects-in-memory.svg)

### 4. The namespace map

The tool's project is `src/tools/louis-agent.rename-projects`, `RootNamespace` `LouisAgent.RenameProjects`, and
its tests `src/tests/tools/louis-agent.rename-projects.tests`. The run renames both, as it renames every project.

| Type or file | Folder | Namespace | Step |
|---|---|---|---|
| `ProjectRename`, `NameMap`, `ListedNames` | `src/tools/louis-agent.rename-projects/Names/` | `LouisAgent.RenameProjects.Names` | 2 |
| `RenameProjectsUserText`, its holes records | `src/tools/louis-agent.rename-projects/Messages/`, `Messages/Holes/` | `LouisAgent.RenameProjects.Messages`, `.Messages.Holes` | 2 |
| `NameMapTests` | `src/tests/tools/louis-agent.rename-projects.tests/Names/` | `LouisAgent.RenameProjects.Tests.Names` | 2 |
| `FixtureRepo`, the small repository the tests build | `src/tests/tools/louis-agent.rename-projects.tests/` | `LouisAgent.RenameProjects.Tests` | 2 |
| `NameRewriter`, `RewriteOutcome`, `LeftoverWords`, `LeftoverWord` | `src/tools/louis-agent.rename-projects/Rewriting/` | `LouisAgent.RenameProjects.Rewriting` | 3 |
| `NameRewriterTests`, `LeftoverWordsTests` | `src/tests/tools/louis-agent.rename-projects.tests/Rewriting/` | `LouisAgent.RenameProjects.Tests.Rewriting` | 3 |
| `TrackedFiles`, `FileMover` | `src/tools/louis-agent.rename-projects/Moving/` | `LouisAgent.RenameProjects.Moving` | 4 |
| `FileMoverTests` | `src/tests/tools/louis-agent.rename-projects.tests/Moving/` | `LouisAgent.RenameProjects.Tests.Moving` | 4 |
| `RenameRun`, `RenameReport`, `Program.cs` | `src/tools/louis-agent.rename-projects/` | `LouisAgent.RenameProjects` | 5 |
| `RenameRunTests` | `src/tests/tools/louis-agent.rename-projects.tests/` | `LouisAgent.RenameProjects.Tests` | 5 |
| `SolutionLayoutTests` (changed by the run) | `src/tests/engine/louis-agent.core.tests/` | as it is | 6 |

### 5. The folders, today and after

```
src/tools/                                   today
  louis-agent.doc-diagrams/   louis-agent.film-export/   louis-agent.usage-probe/

src/tools/                                   after step 2
  louis-agent.doc-diagrams/   louis-agent.film-export/   louis-agent.rename-projects/   louis-agent.usage-probe/

src/engine/                                  after step 6, and every other folder the same way
  LouisAgent.Choices/   LouisAgent.Composition/   LouisAgent.Core/   LouisAgent.Core.Anthropic/   …
    LouisAgent.Core/LouisAgent.Core.csproj

src/docs/projects/                           after step 6
  LouisAgent.Core.md   diagrams/LouisAgent.Core.json   diagrams/LouisAgent.Core-how-it-works.json   …
```

### 6. What is reused

The tool takes its shape from `louis-agent.doc-diagrams`: a `Program.cs` that finds the repository's root by its
solution file, a message enum for every line it prints (ADR 0009), and `MessageText.Format` from
`louis-agent.composition`, with a holes record for each message (ADR 0119). Git is run with its arguments as a list,
never through a shell (ADR 0004). The tests take the repository's paths from `louis-agent.testing`. The diagrams are
redrawn by `louis-agent.doc-diagrams`. No new package.

## Steps

Each step is one commit with its test, written first and seen failing on an assertion. Stop after each for
Louis's review. A step that meets a choice this plan does not settle stops and puts it to him.

### Step 1: the plan is kept

A docs-only step, so it has no test first. It commits this plan and its four pictures, JSON and SVG together,
and the records that go with a plan: a row in the plans' index, a row in the TODO, and the work log's open table
and count.

### Step 2: the tool reads every project's new name

The tool's project and its tests' project are added to the solution and to `SolutionLayoutTests`. `NameMap.Read`
walks the tracked project files and gives each `louis-agent.…` project its new name: its `RootNamespace`, or the
listed name when its namespace is shared. **Tests first:** on a small repository with three projects it reads
three renames; two projects sharing a namespace with no listed name are refused with both named; a listed name
wins over the namespace; a project not named `louis-agent.…` is left out.

### Step 3: the rewriter changes whole names only, and reports the rest

`NameRewriter` replaces the old names with the new, longest first, and `RenameFile` does the same to a file's name.
`LeftoverWords` finds every other `louis-agent.…` word with its line. **Tests first:** `louis-agent.core.web` is
never split by `louis-agent.core`, nor `louis-agent.site-framework-api` by `louis-agent.site`; `louis-agent.dev`
stays; a path in a `ProjectReference` and a name in an `InternalsVisibleTo` both change; `louis-agent.core.md`
and `louis-agent.core-how-it-works.svg` change; `louis-agent.pack.google`, which is no project, is reported and
left.

### Step 4: the mover renames folders and files

`TrackedFiles` reads git's list with its arguments as data. `FileMover` renames every folder and file whose name
begins with a project's name, the deepest first. **Tests first:** on a small git repository, a project's folder
and its project file are renamed, a doc and its picture are renamed, a file inside a renamed folder keeps its own
name, and nothing untracked is touched.

### Step 5: the run, and its check

`RenameRun` puts the three together in the order of the sequence above, and `Program.cs` gives it `--check`.
**Tests first:** a check run on a small repository changes no file and reports what it would do; a run changes the
words, then moves, and its report counts both. The step ends with `--check` run on the real repository, and its
report shown to Louis, before anything is renamed.

### Step 6: the run, one commit

Louis's word first. The tool runs once over the whole repository. Then the solution is built, the build folders
of the old names are cleared, the diagrams tool redraws every picture, the golden files are regenerated if what the
model reads names a project's path, the whole unit suite runs, and the site's containers are rebuilt and looked at
on :5080. One commit, its message naming the tool's report.

### Step 7: the report's leftovers, by hand

Every word in the report is read. The ones a reader follows today are fixed with `Edit` so they follow the new
names: the rules in `.claude/rules/`, the repo map, the guides, the plans and the TODO's planned packs. A name in a
write-up or a fix's lesson, which tells what was true then, is left as it was.

### Step 8: the docs, and the close

The tool gets its row in the projects' index and a short design doc of its own, and its how-to says when to run it.
The repo map and `.claude/README.md` show the new folder names. If Louis says yes, ADR 0052 gains the sentence that
a tool we keep, versioned and tested, is not a home-made script. The write-up goes in skill 18's, the TODO and work
log rows close, and this plan is deleted once its lasting parts are in the tool's doc.

## Verification

The tool's own tests prove each rule on a repository it builds. Before the run, `--check` on the real repository
shows Louis every rename and every leftover word, and writes nothing. After it, the solution builds, the whole unit
suite passes, `doc-diagrams --check` finds no picture stale, and the site on :5080 answers as before. CI on Linux
then proves the one thing Windows can't: that every path's case matches, since Linux's file names are case
sensitive. No paid run.

## Concepts, and where they sit in GOAL.md

- **A tool over hand edits** (steps 2 to 5): a change too wide to trust to a person's hand, made by code that is
  itself tested, then proved by the build and the suite. *Skill 18, testing and shipping.*
- **A name that says what it is** (steps 2 and 6): one name for a project, its DLL, its namespace and its package,
  as .NET's naming guidance asks, ready for F166.2's packages. *Skill 20, extensibility.*

## Approved during the build

---
[Docs index](../README.md) · [Plans](README.md) · Related: [F166](F166-puls-in-its-own-repo.md) · [ADR 0052](../adr/0052-edit-never-script.md) · [ADR 0010](../adr/0010-folders-by-concern.md)
