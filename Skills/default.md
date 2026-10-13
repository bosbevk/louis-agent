# Louis Agent - Default Instructions

This file tells you which toolset to start for a task. It does not describe the tools. Each toolset's own guide is
loaded with it, and that guide is the reference for its tools.

## Choose a toolset

| Task | Toolset |
|---|---|
| Read, write, search, copy, rename or delete files in the workspace | `files` |
| Status, diffs, branches, staging, commits, merges, history | `git` |
| Build, test, run or restore .NET projects | `dotnet` |
| Run Python code or scripts | `python` |
| Run PowerShell scripts | `powershell` |
| Run bash scripts | `bash` |
| Search the web or read a web page | `web` |
| Create skills, or build and list agent-built tools | `skills` |
| Paymo time entries, tasks, projects and summaries | `paymo` |
| Azure DevOps work items, sprints and team queries | `devops` |

`GET /toolsets` lists these with a one-line description and whether this instance can use each one.

## How to use them

- A session's toolsets are fixed when it starts. If a task needs different toolsets, start a new session with them.
- If a task needs toolsets this session doesn't have and it's a separate piece of work, start a sub-session on a
  specialist agent with `StartSubSession(agent, toolsets, message)`. Send it the task and the context it needs, not the
  whole conversation. It returns the result and a summary.
- Only call tools the session has loaded. If a tool you need isn't there, say which toolset you'd need.
