# Prompting Louis Agent for Paymo Time Logging

With `PAYMO_API_KEY` set in `config/.env.secrets`, the agent can log and review your Paymo time. Ask in plain language
from Rider, the web app or the CLI.

## Logging time

The agent logs time with `LogTimeByTaskName`. Give it:

- **The task** — its name, part of its name, its Paymo ticket number, or its numeric task ID.
- **The time** — either hours (`2`, `1.5`) or a start and end time (`08:00`–`09:30`; an end before the start counts as
  crossing midnight).
- **Optional:** notes on what you did, and a date (`YYYY-MM-DD`, default today).

```
Log 2 hours on "Ticket 1234567: Fix login page" – fixed the redirect loop
Log 08:30 to 11:00 on the Checkout Report task with notes "OAuth2 flow"
Log 1.5 hours on task 12345 for 2026-10-03
```

### How the task is found

1. A number is used as the task ID directly.
2. Otherwise Paymo is searched for a task with exactly that name,
3. then for a task whose name **contains** the text,
4. then, if the text contains a 7-digit ticket number, for a task containing that number.

The **first** match is used. With a vague name ("auth") that can be the wrong task, so for anything ambiguous let the
agent search first:

```
Find my Paymo tasks with "checkout" in the name
Log 2 hours on the second one
```

## Looking things up

| Ask for | Tool the agent uses |
|---|---|
| "Find tasks about checkout" | `SearchTasks` |
| "List the tasks in project X" | `ListProjects`, `ListTasks` |
| "Show task 12345" | `GetTaskDetails` |
| "What did I log today / this week?" | `GetTimeEntriesForToday`, `GetTimeEntriesForThisWeek` |
| "List my entries from the 1st to the 5th" | `ListTimeEntries` |
| "Summarise my time by day / project for last week" | `GetDailySummary`, `GetProjectSummary`, `GetUserSummary` |
| "Delete time entry 98765" | `DeleteTimeEntry` (the agent should confirm first) |
| "Who am I in Paymo?" / "Which clients do we have?" | `GetCurrentUser`, `ListClients`, `ListUsers`, `GetCompanyInfo` |

## Tips

1. **Use the ticket number or task ID when you have it** — it's unambiguous.
2. **Give hours or a time range** — if you give both, the time range wins.
3. **Add notes** — they become the entry's description in Paymo.
4. **Check after logging** — "What did I log today?" confirms the entry landed on the right task.
5. **Wrong task?** Ask the agent to delete the entry (it shows the entry id in the list) and log it again.

## Setup

- `PAYMO_API_KEY` in `config/.env.secrets` (create an API key in your Paymo account settings). Without it the Paymo tools aren't loaded.
- Optional: `AGENT_FUNCTION` in `config/.env` adds guidance to the agent's instructions — `time-logging` for
  `time-logging-skills.md`, `paymo` for `paymo-skills.md`, or `louis` for every skill file.
