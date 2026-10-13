# FX10 · A sub-session call without `agent` uses the only peer

> **Status:** done · **Area:** core, the `agents` toolset (`AgentPeerTools.StartSubSession`) · **Found:** 2026-10-13

## Symptom

In the [Puls delegation check with roles](../../samples/sample-output-puls-check-roles/README.md), three of Puls's
`StartSubSession` calls never reached a specialist (API tool log, 2026-10-13):

```
22:07:46 [TOOL] <- StartSubSession failed: ArgumentException: The arguments dictionary is missing a value for the required parameter 'agent'.
22:07:47 [TOOL] <- StartSubSession: Error: no peer agent named 'default'. Peers (AGENT_PEERS): puls.
22:08:33 [TOOL] <- StartSubSession failed: ArgumentException: The arguments dictionary is missing a value for the required parameter 'agent'.
```

Each one cost Puls an extra round (a request that re-reads the conversation) and some seconds before it got the call
right.

## Cause

`agent` was the tool's first parameter, and it was required. Its description already said to leave it empty when
there is one peer, and the tool then used that peer. But a required parameter must still be sent. When Puls left it
out, as the description invited, `FunctionInvokingChatClient`'s argument binding refused the call before the tool ran.
Once Puls had been refused, it guessed a name (`default`).

## Fix

`agent` is optional (`string? agent = null`), so the schema's `required` list is `toolsets` and `message`. Leaving it
out, or sending it empty, picks the only peer, as before. C# needs optional parameters after the required ones, so
`agent` moved behind `message`. The model sends arguments by name, so the order changes nothing for it. The
description and `agents-skills.md` now say "leave it out".

An unknown name is still an error, even with one peer. A wrong name there points at a configuration mistake, and
quietly sending the task elsewhere would hide it.

## Tests

`AgentPeerToolsTests.ACallWithoutAgent_UsesTheOnlyPeer` calls the tool the way the model does, through its
`AIFunction`, with no `agent` argument. It checks that the call reaches the only peer and that the schema requires only
`toolsets` and `message`. The golden file changed for the schema and the guide's line.

## Confirmed

2026-10-14, in the [delegation check after the fixes](../../samples/sample-output-puls-check-fixes/README.md):
all 8 delegations went through on the first call and none named `agent`. The API's tool log has no
`missing a value for the required parameter 'agent'` and no `no peer agent named` lines (the roles run had 3).

## Lesson

A tool's schema is the contract, and the description is only advice. When the description says a value can be left
out, the schema has to agree, or the model gets refused for following the advice. Skill 4 (tool design) in the
[goal](../GOAL.md).
