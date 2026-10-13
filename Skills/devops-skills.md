# DevOps Skills

## Work Item & DevOps Management

### Work Item Queries
- `GetWorkItemsByAssignee(sprint, assignee)` — Get items assigned to a person
- `GetUserStoriesBySprint(sprint)` — Get all user stories in a sprint
- `GetTasksBySprint(sprint)` — Get all tasks in a sprint
- `GetBugsBySprint(sprint)` — Get all bugs in a sprint
- `GetBlockersBySprint(sprint)` — Get all blockers in a sprint
- `GetUnassignedWorkItems(sprint)` — Get unassigned work items
- `GetOpenIssuesBySprint(sprint)` — Get Active/New items (not done)
- `GetWorkItemsByState(state, sprint)` — Get items by state (Active, Resolved, etc.)
- `GetWorkItemById(id)` — Get detailed info for a specific work item

### Work Item Search & Analysis
- `FindWorkItemsByTitle(keyword, sprint, maxResults)` — Search for work items by title
- `GetRecentlyModifiedWorkItems(days, sprint)` — Get items changed in last N days
- `GetSprintSummary(sprint)` — Get overview of all items in sprint (grouped by state)
- `GetTeamWorkloadBySprint(sprint)` — Analyze team member workload
- `GetAvailableSprints()` — List all available sprints

### Work Item Management
- `CreateWorkItem(workItemType, title, description)` — Create a new work item
- `UpdateWorkItem(id, fieldName, fieldValue)` — Update a work item field
- `QueryWorkItems(wiqlQuery)` — Execute custom WIQL (Work Item Query Language) query

### Legacy Skills (via ExecuteSkill)
For backwards compatibility, markdown skills are still available:

```json
ExecuteSkill("GetAllWorkItems", {"sprint": "Sprint 42"})
ExecuteSkill("GetUserStories", {"sprint": "Sprint 42"})
ExecuteSkill("GetWorkItemById", {"id": "12345"})
```

**Note:** Prefer the native DevOps tools above (faster, no subprocess overhead).

## DevOps Workflows

Work item skills: pull the backlog and plan sprints. Keep work visible and tracked.

### Sprint Planning
```
GetAvailableSprints()
GetSprintSummary("Sprint 42")
GetUnassignedWorkItems("Sprint 42")
GetOpenIssuesBySprint("Sprint 42")
```

### Team Workload Review
```
GetTeamWorkloadBySprint("Sprint 42")
GetWorkItemsByAssignee("Sprint 42", "Jane Doe")
GetRecentlyModifiedWorkItems(7, "Sprint 42")
```

### Tracking Progress
```
GetWorkItemsByState("Active", "Sprint 42")
GetWorkItemsByState("Resolved", "Sprint 42")
GetBlockersBySprint("Sprint 42")
```

### Creating & Managing Work
```
CreateWorkItem("Task", "Refactor authentication", "Extract login logic")
UpdateWorkItem("12345", "System.State", "Active")
FindWorkItemsByTitle("exception logging", "Sprint 42")
```
