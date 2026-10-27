# FX25 · The eval runner gets only the settings it reads, not the API's secrets

> **Status:** done · **Area:** API: `Benchmarks/Runs/RunnerEnvironment`, `RunnerStartInfo` · **Found:** 2026-10-26, the Claude project's
> security read of the runner's child process, after F18 step 6a.5 · **Fixed:** 2026-10-27, on the F18 branch (the code it touches, `RunnerStartInfo`, exists only there); Louis's pick of three: the allowlist

## Symptom

Nothing visible: a design gap. The Claude project's read of the runner as an enterprise would see it (2026-10-26): "The runner gets every secret the
API has. .NET hands a child process the parent's full environment by default. The runner only reads about four settings … but it can also see the
Anthropic key, Mongo's login and the admin key. That's the one I'd fix first."

## Cause

`ProcessStartInfo.Environment` starts as a copy of the parent's environment, and `RunnerStartInfo` only added to it (`PULS_API`, `BENCHMARK_RUN`,
the Python flags). So an eval's or a script's process could read `BENCHMARKS_MONGO_URL`, `ADMIN_API_KEY`, `ANTHROPIC_API_KEY`, `LLM_API_KEY` and
every other provider key the container has, none of which it reads. Checked against the code: the runner reads `PULS_API`, `BENCHMARK_RUN`,
`AGENT_API_KEY`, `EVALS`, `CI` and the four `EVAL_JUDGE_*`; through core, for the judge's ledger rows, `LOG_DIRECTORY`, `USAGE_LEDGER`, `PRICES_FILE`
and `PRICES_URL`; an Anthropic judge with no `EVAL_JUDGE_API_KEY` falls back to `ANTHROPIC_API_KEY`. A Python check reads `PULS_API`, `BENCHMARK_RUN`
and `AGENT_API_KEY`.

## Fix

An allowlist, `RunnerEnvironment.Select`: the start info's environment is cleared and refilled with (1) what the runtime needs to start (`PATH`,
`PATHEXT`, `HOME`, `USERPROFILE`, `APPDATA`, `LOCALAPPDATA`, `TEMP`, `TMP`, `TMPDIR`, `SystemRoot`, `SystemDrive`, `ComSpec`, `LANG`, `LC_ALL`, `TZ`,
and every `DOTNET_*`), (2) what the runner reads (the list above), and then, as before, (3) what the API sets for the run. Names match whatever their
case, since Windows keeps `Path`. An Anthropic judge (`EVAL_JUDGE_PROVIDER=anthropic`, or a `claude*` model with no provider named, as the harness's
`JudgeSettings` reads it) with no `EVAL_JUDGE_API_KEY` gets `ANTHROPIC_API_KEY` as `EVAL_JUDGE_API_KEY`: the key it needs, under the judge's name,
never the API's. The three options were an allowlist, a denylist of secret-looking names (weaker: a secret under another name leaks), and an empty
environment (breaks `dotnet`, which needs its path); Louis picked the allowlist.

**What it costs:** a setting the runner starts reading must be added to `RunnerEnvironment.Names`, or it silently never arrives; the guide says so,
and `RunnerEnvironmentTests` pins the list.

## Tests

`RunnerEnvironmentTests` (written first, red on the copied environment): the selection keeps what the runner reads and what the runtime needs and
drops the secrets; an Anthropic judge's key rule, three ways; names matched whatever their case; the start info carries the selection and the run's own
variables; and the real thing, a Python check started by the runner that prints which `*_API_KEY` and `MONGO` names it sees: none.

## Confirmed

The whole solution green after the change, the real runner and the real Python both starting with the allowlisted environment (the re-score and
calibration end-to-end tests run the runner's dll through `dotnet`; the script test runs Python). The container rebuilt; a re-score and a judge
check from the portal still run.

## Lesson

**Least privilege for a child process** (the goal's *Security and guardrails*): what a process can read is part of its interface. A child inherits
the parent's environment unless told otherwise, so "it only reads four settings" is not the same as "it can only see four settings". The allowlist
makes the runner's inputs explicit, which is also documentation: the list *is* what the runner depends on. The paste's other three points (the
runner as its own job with its own identity, sign-in with roles and an audit log, and how long transcripts are kept) are Phase 9's.
