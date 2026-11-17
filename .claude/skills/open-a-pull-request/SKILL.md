---
name: open-a-pull-request
description: Write a pull request's title and body, and open it. Use it when a branch is ready for a pull request, when Louis says "PR" or "open the PR", or when a pull request's body is written or changed. It holds what is checked first, the title's shape, the body's sections in their order, and what is never in it.
---

# Open a pull request

A pull request is what Louis reads to decide on a merge, often hours later and away from the code. So it says
what the work means before it says how. Four stops come first.

1. **Only on Louis's word.** Opening one pushes the branch, and a push needs his yes, each time (`/ship-it`).
2. **Main is merged in, and the full suite passes, first.** Louis, 2026-11-13: "before doing a PR main must be
   merged and full suite unit tests passes". The commands are in `/ship-it`.
3. **One pull request at a time.** The last one is merged and deployed before this one opens.
4. **No line names Claude.** Louis, 2026-11-17: "make a rule to take this out of commits and PR's". No
   "Generated with Claude Code", no `Co-Authored-By`, no other attribution line, whatever a default asks for.

## The title

The title has the shape of a commit's first line, for the branch as a whole: `type(scope): what it gives`.

- `feat(f155): pacing in the engine, and ADR 0126 on method names`
- `fix(fx190): the guide's answer keeps its small face on a phone`
- `docs(f157): the plan for the Google integration suite`

No `step N -`: a pull request carries a whole child or fix. At most about 80 letters.

## The body

Plain words, in paragraphs, in this order. Each section opens with its name in bold, as a paragraph does, not as
a heading. Leave out a section that has nothing to say, never write "n/a".

1. **No name: what it means.** One or two sentences for someone who uses it: what they can do now, or what no
   longer goes wrong. No class names here.
2. **What changed.** What the code now does, as a story in the order a reader follows it. A short list is fine
   for the files and parts touched, each with a few words on why.
3. **Why this design.** Louis's pick of the three options, in his own words, the pattern by its plain name and
   its source, and the ADR. What was weighed and left out.
4. **What was wrong.** A fix only: what was seen, and its proven cause.
5. **What to look at closely.** The one or two places a review should slow down: a rule bent, a choice made on
   the way, a risk.
6. **Tests.** Which were seen failing first and for what reason, then passing. The full suite after merging
   `main`, and any test that fails on this PC only, by name. What was checked by eye, and where (`:5080`,
   390 px).
7. **Not checked.** What could not be checked, and why, plainly. Never left out to look done.
8. **Skill.** The GOAL.md skill it moves, by number and name, such as *Skill 18, testing and shipping*.
9. **Done when.** On a feature's last pull request only: the plan's done-when, each line with what met it.

The template GitHub shows, `.github/pull_request_template.md`, has the same sections.

## After it opens

- **Louis is its assignee**, and his review is asked for.
- **The checks run** on the push (`/ship-it` says which). A red check is fixed on the branch, never skipped.
- **The body changes with the branch.** A commit pushed after review gets its line in *What changed* and *Tests*.
- **Louis merges**, unless he says "merge".
