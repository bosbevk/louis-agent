# Test jobs for draw-a-diagram

Three jobs to try this skill on, in a fresh session that did not write it (`/write-a-skill`, step 7). Each ask is
said the way Louis says it. The skill passes a job when the session loads it from the ask alone, everything in
*Must happen* happens, and nothing in *Must not* does.

| # | Where, and what Louis says | Must happen | Must not |
|---|---|---|---|
| 1 | A project doc with no overview picture. He says: "add the overview diagram" | A `cards` JSON in the doc's `diagrams/` folder in the overview's shape, drawn with the tool, linked under the doc's opening line, and the SVG looked at | An SVG written by hand. A Mermaid block. The JSON committed without its SVG |
| 2 | A plan needs a picture of something kept apart and a lock on it. He says: "nxt" | Every `icon` and `tone` is one of the skill's table, so the picture draws the first time; a box kept apart takes `plain` or the tone its kind says | `lock`, `orange`, `tool`, `check` or `list` tried first. A tone given a meaning the guide doesn't give it |
| 3 | A sequence of four parts. He says: "draw the communication picture" | A `sequence` JSON whose participants each carry the tone that says what they are, drawn, and its key left to the tool | A tone counted on to colour the calls |
