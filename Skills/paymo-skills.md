# Paymo Skills - Time Tracking

Skills for managing time entries and tasks in Paymo.

**Note:** These are placeholder/example skills. For production use, prefer the native **PaymoTools** methods which are faster and don't require subprocess calls:
- `SearchTasks()` - Search for tasks by name/keyword
- `ListTasks()` - List all tasks with filters
- `GetTaskDetails()` - Get task information
- `ListProjects()` - List all projects
- `LogTimeByTaskName()` - Log time to a task (supports duration or time range)
- `ListTimeEntries()` - Get time entries with filters
- And 10+ more native tools for time tracking, users, and analytics

## Skill: LogTime

- Description: Log time to a task by name, hours, or time range
- Parameters:
  - `taskName` (required): Name or ID of the task to log time to
  - `hours` (optional): Number of hours to log (e.g., "1.5")
  - `startTime` (optional): Start time in HH:MM format (e.g., "09:00")
  - `endTime` (optional): End time in HH:MM format (e.g., "10:30")
  - `notes` (optional): Notes or description for the time entry
  - `date` (optional): Date in YYYY-MM-DD format (default: today)
- Execution:

```bash
# This is a placeholder skill
# Implementation delegates to the PaymoTools native function
echo "Time logging for task: $SKILL_ARG_taskName"
```

## Skill: GetTaskList

- Description: Search for tasks in Paymo
- Parameters:
  - `query` (required): Task name or keyword to search for
  - `status` (optional): Filter by status (e.g., "active", "completed")
  - `limit` (optional): Maximum number of results (default: 20)
- Execution:

```bash
# This is a placeholder skill
# Implementation delegates to the PaymoTools native function
echo "Searching for tasks: $SKILL_ARG_query"
```

## Skill: GetTimeEntries

- Description: Retrieve time entries for a date range
- Parameters:
  - `startDate` (required): Start date in YYYY-MM-DD format
  - `endDate` (required): End date in YYYY-MM-DD format
  - `userId` (optional): User ID filter (default: current user)
- Execution:

```bash
# This is a placeholder skill
# Implementation delegates to the PaymoTools native function
echo "Time entries from $SKILL_ARG_startDate to $SKILL_ARG_endDate"
```
