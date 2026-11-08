# louis-agent.identity.triage

> **Explanation.** Who the triage team is and why it is one identity. **Elsewhere:** the workflow in
> [the F106.3 plan](../../../src/docs/plans/F106.3-triage-and-fix-a-service.md); the pack it ends its work with in
> [louis-agent.pack.triage](../../../packs/docs/projects/louis-agent.pack.triage.md); building an identity in
> [Build an identity](../../../src/docs/guides/build-an-identity.md).

A team that triages a service's errors: the **manager** decides what each case gets and checks everyone's work; the
**sorter**, the **investigator** and the **reviewer** work for it; the **coder** is the coding identity on a peer.
One identity with roles (F106.3.2, Louis's pick), because the three only ever work for the manager: the manager's
voice is the mode, the three are `specialists/triage-*.md`, and the packs are names.

## How it works

- `TriageIdentity`: the name `triage`, the voice `triage/voice` as the default, the packs `triage`, `files`, `git`
  and `dotnet` by its own enum.
- `Prompts/agent/triage/voice.md`: the manager as the hub, how it decides (the runbooks come with the case as its
  brief; expected, a person, a fix or a hand-off; a report is a claim until the code checked it), how it ends
  (`DecideCase`, `RaiseToPerson`), and its routing (`triage`, `specialists`).
- The roles arrive with the child's steps 2 to 4.

## Tests

`demos/louis-agent/tests/louis-agent.identity.triage.tests`: the identity's shape, the voice as the roles catalogue
reads it, its routing naming its toolsets.

---
[Docs index](../../../src/docs/README.md) · Related: [Projects](../../../src/docs/projects/README.md)
