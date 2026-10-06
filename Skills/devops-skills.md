# DevOps Skills - Work Item Retrieval

Skills for querying Azure Boards work items using Python.

**Note:** These are legacy Python-based skills. Prefer the native **DevOpsTools** methods, which are faster, have no
subprocess overhead, and default to the team's current sprint:
- `QueryWorkItems()` - Execute WIQL queries
- `GetWorkItemsByAssignee()` - Get items by assignee (empty assignee = you)
- `GetUserStoriesBySprint()` - Get user stories
- `GetTasksBySprint()` - Get tasks
- `GetBugsBySprint()` - Get bugs
- `GetWorkItemById()` - Get single item details
- `GetSprintSummary()` - Sprint overview
- `GetTeamWorkloadBySprint()` - Team workload analysis
- `GetAvailableSprints()` - Sprint names
- And 10+ more native DevOps tools

The organisation and project come from `DEVOPS_ORGANIZATION` and `DEVOPS_PROJECT`; sprints are iterations under the
project, or under `DEVOPS_TEAM` when it is set. The personal access token is `DEVOPS_API_KEY`.

## Skill: GetUserStories

- Description: Retrieve the user stories in a sprint assigned to a person
- Parameters:
  - `sprint` (required): Sprint name, e.g. "Sprint 42" (`GetAvailableSprints` lists them)
  - `assignee` (optional): Display name or email; empty means you (the token's owner)
- Execution:

```bash
python3 << 'PYTHON_EOF'
import os
import json
import urllib.request
import urllib.error
import urllib.parse
import base64
import sys

api_key = os.environ.get('DEVOPS_API_KEY', '')
organization = os.environ.get('DEVOPS_ORGANIZATION', '')
project = os.environ.get('DEVOPS_PROJECT', '')
team = os.environ.get('DEVOPS_TEAM', '')
sprint = os.environ.get('SKILL_ARG_sprint', '')
assignee = os.environ.get('SKILL_ARG_assignee', '')

if not (organization and project):
    sys.stdout.write(json.dumps({"error": "Set DEVOPS_ORGANIZATION and DEVOPS_PROJECT"}))
    sys.exit(1)
if not sprint:
    sys.stdout.write(json.dumps({"error": "sprint is required (GetAvailableSprints lists them)"}))
    sys.exit(1)

iteration = "\\\\".join(part for part in (project, team, sprint) if part)
assigned = "[System.AssignedTo] = @Me" if not assignee else f"[System.AssignedTo]='{assignee}'"
query = {
    "query": f"SELECT [System.Id], [System.Title], [System.State], [System.IterationPath] FROM workitems WHERE [System.TeamProject]='{project}' AND [System.WorkItemType]='User Story' AND [System.IterationPath] Under '{iteration}' AND {assigned}"
}

url = f"https://dev.azure.com/{urllib.parse.quote(organization)}/{urllib.parse.quote(project)}/_apis/wit/wiql?api-version=7.0"
headers = {
    "Content-Type": "application/json"
}

# Create auth header: ":api_key" base64 encoded
auth_string = f":{api_key}"
auth_bytes = auth_string.encode('utf-8')
auth_b64 = base64.b64encode(auth_bytes).decode('ascii')
headers["Authorization"] = f"Basic {auth_b64}"

req = urllib.request.Request(url, data=json.dumps(query).encode('utf-8'), headers=headers, method='POST')
try:
    with urllib.request.urlopen(req) as response:
        result = json.loads(response.read().decode('utf-8'))
        sys.stdout.write(json.dumps(result, ensure_ascii=False, indent=2))
except urllib.error.HTTPError as e:
    error_body = e.read().decode('utf-8')
    sys.stdout.write(json.dumps({"error": error_body, "code": e.code}, ensure_ascii=False))
except Exception as e:
    sys.stdout.write(json.dumps({"error": str(e)}, ensure_ascii=False))
PYTHON_EOF
```

## Skill: GetAllWorkItems

- Description: Retrieve all work items in a sprint assigned to a person (not filtered by type)
- Parameters:
  - `sprint` (required): Sprint name, e.g. "Sprint 42" (`GetAvailableSprints` lists them)
  - `assignee` (optional): Display name or email; empty means you (the token's owner)
- Execution:

```bash
python3 << 'PYTHON_EOF'
import os
import json
import urllib.request
import urllib.error
import urllib.parse
import base64
import sys

api_key = os.environ.get('DEVOPS_API_KEY', '')
organization = os.environ.get('DEVOPS_ORGANIZATION', '')
project = os.environ.get('DEVOPS_PROJECT', '')
team = os.environ.get('DEVOPS_TEAM', '')
sprint = os.environ.get('SKILL_ARG_sprint', '')
assignee = os.environ.get('SKILL_ARG_assignee', '')

if not (organization and project):
    sys.stdout.write(json.dumps({"error": "Set DEVOPS_ORGANIZATION and DEVOPS_PROJECT"}))
    sys.exit(1)
if not sprint:
    sys.stdout.write(json.dumps({"error": "sprint is required (GetAvailableSprints lists them)"}))
    sys.exit(1)

iteration = "\\\\".join(part for part in (project, team, sprint) if part)
assigned = "[System.AssignedTo] = @Me" if not assignee else f"[System.AssignedTo]='{assignee}'"
query = {
    "query": f"SELECT [System.Id], [System.Title], [System.State], [System.WorkItemType], [System.IterationPath] FROM workitems WHERE [System.TeamProject]='{project}' AND [System.IterationPath] Under '{iteration}' AND {assigned}"
}

url = f"https://dev.azure.com/{urllib.parse.quote(organization)}/{urllib.parse.quote(project)}/_apis/wit/wiql?api-version=7.0"
headers = {
    "Content-Type": "application/json"
}

# Create auth header: ":api_key" base64 encoded
auth_string = f":{api_key}"
auth_bytes = auth_string.encode('utf-8')
auth_b64 = base64.b64encode(auth_bytes).decode('ascii')
headers["Authorization"] = f"Basic {auth_b64}"

req = urllib.request.Request(url, data=json.dumps(query).encode('utf-8'), headers=headers, method='POST')
try:
    with urllib.request.urlopen(req) as response:
        result = json.loads(response.read().decode('utf-8'))
        sys.stdout.write(json.dumps(result, ensure_ascii=False, indent=2))
except urllib.error.HTTPError as e:
    error_body = e.read().decode('utf-8')
    sys.stdout.write(json.dumps({"error": error_body, "code": e.code}, ensure_ascii=False))
except Exception as e:
    sys.stdout.write(json.dumps({"error": str(e)}, ensure_ascii=False))
PYTHON_EOF
```

## Skill: GetWorkItemById

- Description: Get detailed information about a specific work item
- Parameters:
  - `id` (required): Work item ID
- Execution:

```bash
python3 << 'PYTHON_EOF'
import os
import json
import urllib.request
import urllib.error
import urllib.parse
import base64
import sys

api_key = os.environ.get('DEVOPS_API_KEY', '')
organization = os.environ.get('DEVOPS_ORGANIZATION', '')
project = os.environ.get('DEVOPS_PROJECT', '')
work_item_id = os.environ.get('SKILL_ARG_id', '')

if not (organization and project):
    sys.stdout.write(json.dumps({"error": "Set DEVOPS_ORGANIZATION and DEVOPS_PROJECT"}))
    sys.exit(1)
if not work_item_id.isdigit():
    sys.stdout.write(json.dumps({"error": "id must be a work item number"}))
    sys.exit(1)

url = f"https://dev.azure.com/{urllib.parse.quote(organization)}/{urllib.parse.quote(project)}/_apis/wit/workitems/{work_item_id}?api-version=7.0&$expand=all"
headers = {}

# Create auth header: ":api_key" base64 encoded
auth_string = f":{api_key}"
auth_bytes = auth_string.encode('utf-8')
auth_b64 = base64.b64encode(auth_bytes).decode('ascii')
headers["Authorization"] = f"Basic {auth_b64}"

req = urllib.request.Request(url, headers=headers, method='GET')
try:
    with urllib.request.urlopen(req) as response:
        result = json.loads(response.read().decode('utf-8'))
        sys.stdout.write(json.dumps(result, ensure_ascii=False, indent=2))
except urllib.error.HTTPError as e:
    error_body = e.read().decode('utf-8')
    sys.stdout.write(json.dumps({"error": error_body, "code": e.code}, ensure_ascii=False))
except Exception as e:
    sys.stdout.write(json.dumps({"error": str(e)}, ensure_ascii=False))
PYTHON_EOF
```
