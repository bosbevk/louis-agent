# FX15 · A test passed only on a machine with `config/.env`

> **Status:** done · **Area:** tests, `DisplayCurrencyTests` (and how `AgentHost` finds `config/`) · **Found:** 2026-10-14 ·
> **Fixed:** 2026-10-14

## Symptom

The first CI run ([the workflow](../../.github/workflows/ci.yml), on a fresh Ubuntu clone) failed one test of 988:

```
Failed AgentHostBuild_HostCultureEnZa_EveryLineInRands
  Expected: ("ZAR", "R", "USD")
  But was:  ("USD", "$", "USD")
```

with `[WARN] No exchange rates at config/rates.json: costs are shown in USD` and
`[WARN] No price table at config/prices.json` in the log. Both files are tracked and were in the checkout.

## Cause

`AgentHost` finds the default `config/` folder with `EnvFile.FindConfigDirectory`, which walks up from the current
directory and accepts a `config/` only when it holds a `.env` or `.env.secrets`. Both are gitignored, so on a fresh
clone there is no `config/` as far as the host is concerned, and the rates and prices fall back to "none". The test
relied on that discovery, so it passed on every developer machine (which has a `.env`) and nowhere else. Reproduced
locally by moving `config/.env` and `config/.env.secrets` aside for one run: the same failure.

## Fix

The test names the files it means: `agent.RatesFile` and `agent.PricesFile` point at the tracked `config/rates.json`
and `config/prices.json` under `TestPaths.RepoRoot`. The test is about resolving the display currency, not about
finding the config folder, so it shouldn't depend on an untracked file. `FindConfigDirectory` itself is unchanged: for
a host, "a `config/` with a `.env`" is the right rule, since that is how the settings are found too.

## Tests

The test itself, run with the two env files moved aside: red before the change, green after; green with them in place
too.

## Confirmed

The CI run after the fix (this PR's second run) is green on Ubuntu.

## Lesson

A test that reads from the machine instead of from what it was given passes where it was written and fails where it is
first run elsewhere. CI's first job was to find the hidden local state, which is exactly why it exists. Skill 12
(evaluation) in the [goal](../GOAL.md): a check is only a check if it runs the same everywhere.
