# Paymo Skills

## Time & Project Management (Paymo)

### Time Entry Management
- `LogTimeByTaskName(taskName, startTime, endTime, hours, notes, date)` — Log time to a task
- `GetTimeEntriesForToday()` — Get all time entries logged today
- `GetTimeEntriesForThisWeek()` — Get time entries from Monday to Sunday
- `ListTimeEntries(startDate, endDate, userId, maxResults)` — List entries with filters
- `GetTimeEntryDetails(entryId)` — Get specific time entry details
- `DeleteTimeEntry(entryId)` — Delete a time entry

### Task Management
- `SearchTasks(query, status, maxResults)` — Search for tasks by name/keyword
- `ListTasks(projectId, status, maxResults)` — List all tasks (optionally filtered)
- `GetTaskDetails(taskId)` — Get detailed information about a task

### Project Management
- `ListProjects(status, maxResults)` — List all projects (active, archived)
- `GetProjectDetails(projectId)` — Get detailed project information

### Time Analytics & Reports
- `GetDailySummary(startDate, endDate)` — Time entry summary grouped by day
- `GetUserSummary(startDate, endDate)` — Time entry summary grouped by user
- `GetProjectSummary(startDate, endDate)` — Time entry summary grouped by project

### Team & Organization
- `ListClients(maxResults)` — List all clients
- `ListUsers(maxResults)` — List team members
- `GetCurrentUser()` — Get authenticated user's info
- `GetCompanyInfo()` — Get company/workspace information

## Paymo Workflows

### Time Tracking & Reporting
```
# Log time to a task
LogTimeByTaskName("Ticket 1234567: Fix login page", "09:00", "10:30", "", "Fixed report layout")

# Review your time entries
GetTimeEntriesForToday()
GetTimeEntriesForThisWeek()

# Get time analytics
GetDailySummary("2026-10-01", "2026-10-05")
GetUserSummary("2026-10-01", "2026-10-05")
GetProjectSummary("2026-10-01", "2026-10-05")
```

### Task & Project Lookup
```
# Search for tasks
SearchTasks("authentication", "active", 20)
GetTaskDetails("12345")

# List all projects
ListProjects("active")
GetProjectDetails("999")
```

### Team Capacity & Workload
```
# Check who's working on what
ListUsers()
GetUserSummary("2026-10-01", "2026-10-05")
GetCompanyInfo()
```

