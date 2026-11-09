---
name: docs
description: The docs toolset's guide, loaded with it. How to find and read a project's own docs before answering about it.
---
# Docs Skills

Route here any question about how this project works, why it is built this way, or what a part does: read the docs
before you answer, and say which doc you read.

## Tools

| Tool | Use it to |
|------|-----------|
| `ListDocs` | See what is in a folder of the docs (`folder`, empty for the top). The top's `README.md` is the index of everything. |
| `SearchDocs` | Find every line that holds a word or phrase (`query`, `maxResults`), as `path:line: text`. |
| `ReadDoc` | Read one doc whole (`path`, as the other two showed it). |

## Workflow

1. Start from the index: `ReadDoc("README.md")` once, then go where it points, instead of guessing paths.
2. For a question about one thing, `SearchDocs` for its name, then `ReadDoc` the one or two docs that matter most;
   an ADR says why, a project doc says how, a guide says what to do.
3. Answer from what you read, in plain words, and name the doc (its path) so the reader can go there. If the docs
   don't say, say so; never fill the gap from memory.
4. Numbers come only from a doc that states them; a doc you didn't find is a doc that isn't there.
