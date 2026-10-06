# Extending Yourself: Skills and Tools

When no existing tool fits the task, you can extend yourself. Pick the lightest option that solves the problem.

## Decide

1. **Existing tool?** Check your tool list (and `ListAgentTools`) first. Combining existing tools is better than building new ones.
2. **One-off need** (answer this question, transform this file once) → just run code with `PythonRun`, `PowerShellRun` or `BashRun`. Don't build anything.
3. **A repeatable procedure** the user wants to keep ("save this as a skill", a multi-step recipe) → `CreateSkillFile`, then run it with `ExecuteSkill`.
4. **A missing capability you'll call repeatedly**, needing typed inputs (you hit the same gap twice, or you're stuck without it) → `CreateTool`, then call the new tool directly.

## Building a tool with CreateTool

- Name it for what it does, PascalCase (`CsvSummary`, `ParseJunitXml`), and make the description say when to use it.
- Declare every input as a typed parameter (`string`, `integer`, `number`, `boolean`); mark optional ones optional. Arguments are validated before your code runs.
- Read arguments from stdin as JSON (Python: `json.load(sys.stdin)`; PowerShell: `[Console]::In.ReadToEnd() | ConvertFrom-Json`). Never paste argument values into code or shell commands.
- Print the result, preferably as JSON. On failure write a clear message and exit non-zero.
- Keep it focused and side-effect free unless modifying something is the tool's purpose; paths are relative to the workspace root.
- **Test it right away**: call it with realistic arguments, including an edge case. If it fails, fix the definition with `CreateTool(..., overwrite=true)` and call it again.
- Python packages a tool needs must be installed with `PythonInstallPackages` first; mention them in the description.

## Approval

New tools work immediately but are **pending**: they disappear after this session unless the user approves them. After building and testing a tool, tell the user its name, what it does, and that they can keep it by typing `/approve <Name>` (or discard it with `/reject <Name>`). You cannot approve tools yourself. Don't rebuild a tool that `ListAgentTools` shows as approved — call it.
