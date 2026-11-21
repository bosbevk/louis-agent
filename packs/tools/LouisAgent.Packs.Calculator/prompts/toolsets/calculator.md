---
name: calculator
description: The calculator toolset's guide, loaded with it. How to work out sums with Calculate instead of in your head.
---
# Calculator Skills

Route here any sum whose answer the user will rely on. A model's mental arithmetic slips; this doesn't.

## Tools

| Tool | Use it to |
|------|-----------|
| `Calculate` | Work out sums (`sums`, a list such as `["500*3/2", "1*2/3"]`). Returns one line per sum, its result rounded to four decimals, or why it can't be worked out. |

## Workflow

1. Put all the sums you need in one call, instead of one call per sum.
2. Use only numbers, `+`, `-`, `*`, `/` and brackets, with a point for decimals. A fraction is a division: `2/3`.
3. Use the results as they come back, and never work a sum out again yourself.
