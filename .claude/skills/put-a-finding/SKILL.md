---
name: put-a-finding
description: Put a choice to Louis with three options and wait for his pick. Use it whenever work turns up something the plan or the brief doesn't settle, such as a design point, how to shape a class, which package to add, a rule that clashes with what was asked, a layout or naming question, or a test that can't be written as asked.
---

# Put a finding to Louis

A design choice is never Claude's call. Four stops come first.

1. **Don't choose.** Stop, put it to Louis, and wait for his pick. Nothing that depends on the choice is built
   first.
2. **One finding at a time.** Each on its own, with three options.
3. **Never add a package before asking.** Louis: "when choosing a package ask me first as a rule give me three
   options pros and cons". That holds for a test project's package too. Never `dotnet add package` first and ask
   later.
4. **Research before you ask.** A finding put without research is a guess. The repo may already hold the thing you
   were about to build, and then the first option is to use it.

## Is it a finding?

**Yes:** a design point. How to shape a class, pick an implementation or hold a rule. Any package. A rule that
clashes with the brief. A layout or naming question. A test that can't be written as asked.

**No:** a mechanical call, such as a local variable's name or a test's wording.

## How to put it

The order matters. The research comes before anything is said, so the recommendation rests on it.

1. **Research first.** Before you write a word to Louis:
   - Read the code for what is there already, and how the repo solved the nearest problem.
   - Look up the patterns that fit: Refactoring.Guru, the Gang of Four, Fowler, Microsoft's .NET guidance. Never
     the first pattern that comes to mind.
   - For a package, find its licence (MIT, Apache or BSD preferred), how it is maintained, its size, and how well
     it fits.

   Nothing in a finding comes from memory. What you could not find out, say so.
2. **Say what you found**, in plain words: what it means first, then what in the code makes it a choice.
3. **Give three options, and explain each one.** An option is not a label. For each, say:
   - what it is, with its pattern named in plain words
   - how it would work here, in this code
   - what it changes
   - what it costs
   - where the research found it, so Louis can look
4. **Recommend one, from the research.** Put it first. Say why it fits this case better than the other two, and
   name what you found that points to it. A recommendation with no reason from the research is a guess.
5. **Wait for his pick.**

## What Louis has already said he prefers

Offer these first, where they fit.

- **Reuse before building.** Louis, 2026-10-13: "use 3rd party over creating our own. Only create our own if there
  is no third party to fulfil our needs". And before a package, what the repo already has.
- **A choice about a look is made by looking.** Build the options as real pages he can open, not as descriptions.

## After his pick

- **Name the pick** in the commit, in the explanation of the step, and in the `<remarks>` of each class that
  carries it, with what it was chosen over (ADR 0050).
- **If it changes an approved plan**, add it under the plan's *Approved during the build*, with the date and his
  words.
- **If later work must follow it**, it wants an ADR. Propose the ADR in a few lines, the rule and its reason, and
  wait for his yes. No ADR is written without it.
