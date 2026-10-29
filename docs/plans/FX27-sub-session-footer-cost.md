# FX27 · A sub-session's answer carries no usage: plan

> Approved 2026-10-29 (Louis's brief, with step 2 changed after a finding) · Branch:
> `fix/FX27-sub-session-footer-cost`, from FX26's tip (Louis's pick: FX26 is not on main yet, so FX27 merges after
> it) · Fix: [FX27](../fixes/FX27-sub-session-footer-cost.md)

## Context

A specialist's (and a minion's) answer comes back to the parent as a tool result with a footer:
`[Sub-session on coder (files, git): 3 requests, 10,200 tokens, 0.0123 USD]` (`SubSessionToolMessage.SubSessionAnswer`,
`SubSessionAnswerKept`, `MinionAnswer`, `SubSessionNoAnswer`, built by `SubSessionUsage.Line()`). That text is part of
the parent's next model request. On a replay the child's usage is zero, so the text differs, the request's hash
differs, and the replay misses at the parent's next request. That is the cause behind FX26's misses, the turn-2 miss
on `cook-af` and `cook-en` included.

The cost line is also against our own rule: the ledger is the only source of cost (ADR 0007), and the model gains
nothing from knowing what its specialist cost. The usage itself is already rolled up into the parent's turn and the
ledger (`UsageRollUp.Record`, one `UsagePurpose.SubSession` row per reply); nothing is lost by taking it out of the
prompt.

## Design (Louis's pick, 2026-10-29)

- **The usage leaves every tool result text** (pick 1 of 3: remove what changes between runs from what the model
  reads). The other two, masking it in the replay's key and replaying the usage too, are in the fix doc with why not.

## Steps

One commit each, shown before it is committed; never pushed without asking.

0. **The fix doc.** `docs/fixes/FX27-sub-session-footer-cost.md` (symptom, cause, fix, how it was confirmed, the
   three options and why option 1), its line in `docs/fixes/README.md`, this plan. Commit `docs(fx27): the fix doc`.
1. **Test first, then the fix.** In `tests/louis-agent.core.tests/Tools/Agents/SpecialistToolsTests.cs` (and the
   minion one if it asserts the footer): a reply whose usage is all zeros gives **the same tool result text** as the
   same reply with real usage. Run it, see it fail on the footer, keep the output.
   Then **the usage line leaves every tool result text.** The footer keeps only what the model needs to find the
   sub-session again: `[Sub-session on coder (files, git)]`, `[Sub-session s1 on puls (web; cook). It stays open: …]`,
   `[Minion cook on haiku with ReadRenderedPage]`, and `SubSessionNoAnswer` without its `[{Usage}]`.
   `SubSessionUsage` stays (the roll-up needs it); `Line()` and the `SubSessionUsage`, `SubSessionNoUsage`,
   `SubSessionCostUnknown` enum members go, `SubSessionReply` loses `UsageLine`, and the `SpecialistTools`
   description stops promising "a usage line". One `[TOOL]` log line at Information with the numbers is fine: logs
   are not the prompt. Update the assertions that quote the footer, regenerate the goldens
   (`ToolMessageSnapshot.json` and the model-request snapshots, `LOUIS_UPDATE_GOLDEN=1`) and review their diff: only
   footers and the tool description change. Commit `fix(fx27): the sub-session footer carries no usage`.
2. **Confirm it.** The baseline recording (`5d1fd231c9b1`) holds the parent's requests with the old footer, so a
   replay of it misses the other way and can't confirm FX27. Instead: step 1's test (the parent's request is the same
   whether the child's usage is zero or real), then one fresh recorded cook run after FX27 is in, a paid run that
   waits for Louis's go, and a free replay of it, in which `cook-af` and `cook-en` should hit at every turn. The run
   ids and the outcome go under *Confirmed* in the fix doc, with the note that a replayed sub-session reply is now
   byte-identical to the recorded one (the fix doc only, Louis's pick). Commit
   `docs(fx27): confirmed on a fresh recording`. Then stop and ask before pushing.

## Done when

A replay of a delegating run finds every parent request in its recording: the specialist's answer reads the same
whatever the child used, and the usage is still in the ledger and the parent's turn.

### Types

| Type | New / changed | Folder | Namespace | Step |
|---|---|---|---|---|
| `SubSessionToolMessage` | changed: footers without `{Usage}`; three members go | `louis-agent.core/Tools/Agents/Messages/` | `LouisAgent.Core.Tools.Agents.Messages` | 1 |
| `SubSessionUsage` | changed: `Line()` goes | `louis-agent.core/Tools/Agents/SubSessions/` | `LouisAgent.Core.Tools.Agents.SubSessions` | 1 |
| `SubSessionMessenger.SubSessionReply` | changed: `UsageLine` goes | `louis-agent.core/Tools/Agents/SubSessions/` | `LouisAgent.Core.Tools.Agents.SubSessions` | 1 |
| `SpecialistTools`, `MinionTools` | changed: footers and description | `louis-agent.core/Tools/Agents/` | `LouisAgent.Core.Tools.Agents` | 1 |

## Approved during the build

(nothing yet)
