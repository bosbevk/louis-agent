# Hello World Skills

Example skills for testing agent skill execution.

## Overview

These are simple test skills used for development and testing. Use them to:
- Verify skill execution works correctly
- Test parameter substitution
- Confirm output formatting

These skills are available when `AGENT_FUNCTION=HelloWorld` or when loaded explicitly for testing.

## Guidelines

- **Parameter substitution:** Parameters are passed as environment variables with the `SKILL_ARG_` prefix (e.g., `$SKILL_ARG_paramName`). Use bash parameter expansion like `${SKILL_ARG_name:-default}` for defaults.
- **Output:** Results are printed to stdout and returned as-is.
- **Errors:** Exit codes ≠ 0 produce error messages; the agent sees them.

## Constraints

- These are test skills for development only.
- Not intended for production use.
- File paths are relative to the workspace root.

## Skill: SayHello

- Description: Greet the user with a simple echo message
- Parameters:
  - `name` (string): Name to greet (default: "World")
- Execution:

```bash
echo "Hello, ${SKILL_ARG_name:-World}!"
```

## Skill: GetCurrentTime

- Description: Print the current system date and time
- Parameters: (none)
- Execution:

```bash
date
```

## Skill: ListFiles

- Description: List files and directories at a given path
- Parameters:
  - `directory` (string): Path to list (default: ".")
- Execution:

```bash
ls -la "${SKILL_ARG_directory:-.}"
```
