---
name: write-a-skill
description: The standard a skill is written in, for CreateSkill. Read it before you make a skill or change one.
metadata:
  load: on-demand
---
# Write a skill

A skill is a folder the person can read and approve: a `SKILL.md`, a JSON Schema of its parameters, and one script.
Make it with `CreateSkill`, which keeps it only if its example call runs cleanly.

## The name

A verb and what it acts on, in kebab case: lowercase letters, digits and hyphens, at most 64 characters, such as
`count-words` or `convert-currency`. It is the folder's name, and the `name` in the header says the same.

## The SKILL.md

It opens with YAML front matter, then a body.

```
---
name: count-words
description: Counts the words in a text. Use it when the person asks how long a text is.
metadata:
  script: scripts/count.py
---
# Count words

Give it the text. It answers with the number of words, nothing else.
```

- **description:** in the third person, what it does and then when to use it, under 1,024 characters. Write it
  without a colon followed by a space, which YAML reads as a new field; say "for example" instead.
- **metadata.script:** the script's path inside the folder, a `.py`, `.ps1` or `.sh` file.
- **The body:** what the skill is for, what it needs, and what it gives back, in a few plain lines.

## The parameters

A JSON Schema object: `type` is `object`, each parameter sits under `properties` with its `type` (string, integer,
number, boolean, array or object) and a `description`, and the ones a call must give are under `required`.

## The script

It reads its arguments as one JSON object from stdin and prints its answer. It never builds a shell command from
an argument and never runs one; it reads the value as data. It does one thing, finishes within a minute, and prints
plain text the person can read.

## The example

One call's arguments, a JSON object that fits the parameters. `CreateSkill` runs it, so pick a value that shows the
skill working, not an edge case. If it fails, nothing is kept, and the answer says what to fix.
