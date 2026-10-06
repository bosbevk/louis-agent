# Time Logging Skills

Skills for time entry management and time tracking workflows. These are placeholder/example skills (each `echo`s
a message rather than logging real time) kept to demonstrate the skill format — prefer the native `PaymoTools`
methods below, which are faster and more reliable.

## Tools

| Tool | Use it to |
|------|-----------|
| `LogTimeByTaskName` | Log time with flexible input (duration or time range) |
| `GetTimeEntriesForToday` / `GetTimeEntriesForThisWeek` | Get today's / this week's entries |
| `ListTimeEntries` | List entries with date/user filters |
| `GetDailySummary` / `GetUserSummary` / `GetProjectSummary` | Time summary grouped by day / user / project |
| `DeleteTimeEntry` | Remove a time entry |

## Skill: LogWorkEntry

- Description: Log a work time entry with flexible time input
- Parameters:
  - `description` (required): Description of the work performed
  - `duration` (optional): Duration in hours (e.g., "1.5", "2")
  - `startTime` (optional): Start time in HH:MM format
  - `endTime` (optional): End time in HH:MM format
  - `date` (optional): Date in YYYY-MM-DD format (default: today)
  - `taskId` (optional): Associated task ID
- Execution:

```bash
# This is a placeholder skill
# Implementation delegates to time logging tools
echo "Logging work entry: $SKILL_ARG_description"
```

## Skill: GetWeeklySummary

- Description: Get a summary of time logged this week
- Parameters:
  - `groupBy` (optional): Group results by (project, task, day)
  - `includeNotes` (optional): Include notes in summary (true/false)
- Execution:

```bash
# This is a placeholder skill
# Implementation delegates to time summary tools
echo "Weekly summary grouped by: $SKILL_ARG_groupBy"
```

## Skill: ValidateTimeEntry

- Description: Validate time entry before logging
- Parameters:
  - `startTime` (optional): Start time in HH:MM format
  - `endTime` (optional): End time in HH:MM format
  - `duration` (optional): Duration in hours
- Execution:

```bash
# This is a placeholder skill
# Implementation delegates to time validation tools
echo "Validating time entry"
```
